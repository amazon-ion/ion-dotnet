using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using IonDotnet.Conversions;
using IonDotnet.Internals.Binary;

namespace IonDotnet.Serialization
{
    /// <summary>
    /// Deserializer
    /// </summary>
    public static partial class IonSerialization
    {
        /// <summary>
        /// Deserialize a binary format to object type T
        /// </summary>
        /// <param name="binary">Binary input</param>
        /// <param name="scalarConverter"></param>
        /// <typeparam name="T">Type of object to deserialize to</typeparam>
        /// <returns>Deserialized object</returns>
        public static T Deserialize<T>(byte[] binary, IScalarConverter scalarConverter = null)
        {
            using (var stream = new MemoryStream(binary))
            {
                var reader = new UserBinaryReader(stream, scalarConverter);
                reader.MoveNext();
                return (T) Deserialize(reader, typeof(T), scalarConverter);
            }
        }

        private static object Deserialize(IIonReader reader, Type type, IScalarConverter scalarConverter)
        {
            object t = null;

            if (TryDeserializeScalar(reader, type, scalarConverter, ref t)) return t;
            if (TryDeserializeCollection(reader, type, scalarConverter, ref t)) return t;

            //object
            t = Activator.CreateInstance(type);
            reader.StepIn();

            while (reader.MoveNext() != IonType.None)
            {
                //find the property
                var prop = type.GetProperty(reader.CurrentFieldName, BindingFlags.Public | BindingFlags.Instance);
                if (prop == null) continue;
                if (!prop.CanWrite) throw new IonException($"Property {type.Name}.{prop.Name} cannot be set");

                var propValue = Deserialize(reader, prop.PropertyType, scalarConverter);
                prop.SetValue(t, propValue);
            }

            reader.StepOut();
            return t;
        }

        private static bool TryDeserializeCollection(IIonReader reader, Type type, IScalarConverter scalarConverter, ref object result)
        {
            if (!typeof(IEnumerable).IsAssignableFrom(type)) return false;

            //special case of byte array
            if (TryDeserializeByteArray(reader, type, scalarConverter, ref result)) return true;

            if (reader.CurrentType != IonType.List) return false;

            //figure out collection type
            Type elementType, constructedListType = null;
            if (type.IsArray)
            {
                elementType = type.GetElementType();
            }
            else
            {
                var generics = type.GetGenericArguments();
                if (generics.Length == 0) throw new IonException("Must specify collection type");
                var listType = typeof(List<>);
                elementType = generics[0];
                constructedListType = listType.MakeGenericType(elementType);
                if (!type.IsAssignableFrom(constructedListType)) throw new IonException("Must be collection");
            }

            reader.StepIn();
            var arrayList = new ArrayList();

            while (reader.MoveNext() != IonType.None)
            {
                var element = Deserialize(reader, elementType, scalarConverter);
                arrayList.Add(element);
            }

            if (type.IsArray)
            {
                var arr = Array.CreateInstance(elementType, arrayList.Count);
                for (var i = 0; i < arrayList.Count; i++)
                {
                    arr.SetValue(arrayList[i], i);
                }

                result = arr;
            }
            else
            {
                var list = (IList) Activator.CreateInstance(constructedListType);
                foreach (var item in arrayList)
                {
                    list.Add(item);
                }

                result = list;
            }

            reader.StepOut();
            return true;
        }

        private static bool TryDeserializeByteArray(IIonReader reader, Type type, IScalarConverter scalarConverter, ref object result)
        {
            if (!type.IsAssignableFrom(typeof(byte[]))) return false;
            if (reader.CurrentType != IonType.Blob && reader.CurrentType != IonType.Clob) return false;

            var bytes = new byte[reader.GetLobByteSize()];
            reader.GetBytes(bytes);
            result = bytes;
            return true;
        }

        private static bool TryDeserializeScalar(IIonReader reader, Type type, IScalarConverter scalarConverter, ref object result)
        {
            if (type == typeof(string))
            {
                if (reader.CurrentType != IonType.String && reader.CurrentType != IonType.Null) return false;
                result = reader.CurrentIsNull ? null : reader.StringValue();
                return true;
            }

            if (reader.CurrentIsNull)
            {
                if (type.IsValueType) return false;

                result = null;
                return true;
            }

            //check for enum/symbol
            if (type.IsEnum)
            {
                if (reader.CurrentType != IonType.Symbol) goto NoMatch;
                var symbolText = reader.SymbolValue().Text;
                return Enum.TryParse(type, symbolText, out result);
            }

            if (type == typeof(bool))
            {
                if (reader.CurrentType != IonType.Bool) goto NoMatch;
                result = reader.BoolValue();
                return true;
            }

            if (type == typeof(int))
            {
                if (reader.CurrentType != IonType.Int) goto NoMatch;
                switch (reader.GetIntegerSize())
                {
                    case IntegerSize.Int:
                        result = reader.IntValue();
                        return true;
                    case IntegerSize.Long:
                    case IntegerSize.BigInteger:
                        throw new OverflowException($"Encoded value is too big for int32");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (type == typeof(long))
            {
                if (reader.CurrentType != IonType.Int) goto NoMatch;
                switch (reader.GetIntegerSize())
                {
                    case IntegerSize.Int:
                        result = (long) reader.IntValue();
                        return true;
                    case IntegerSize.Long:
                        result = reader.LongValue();
                        return true;
                    case IntegerSize.BigInteger:
                        throw new OverflowException($"Encoded value is too big for int32");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (type == typeof(BigInteger))
            {
                if (reader.CurrentType != IonType.Int) goto NoMatch;
                switch (reader.GetIntegerSize())
                {
                    case IntegerSize.Int:
                        result = new BigInteger(reader.IntValue());
                        return true;
                    case IntegerSize.Long:
                        result = new BigInteger(reader.LongValue());
                        return true;
                    case IntegerSize.BigInteger:
                        result = reader.BigIntegerValue();
                        return true;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (type == typeof(float))
            {
                if (reader.CurrentType != IonType.Float) goto NoMatch;
                result = (float) reader.DoubleValue();
                return true;
            }

            if (type == typeof(double))
            {
                if (reader.CurrentType != IonType.Float) goto NoMatch;
                result = reader.DoubleValue();
                return true;
            }

            if (type == typeof(decimal))
            {
                if (reader.CurrentType != IonType.Decimal) goto NoMatch;
                result = reader.DecimalValue();
                return true;
            }

            if (type == typeof(DateTime))
            {
                if (reader.CurrentType != IonType.Timestamp) goto NoMatch;
                result = reader.TimestampValue().DateTimeValue;
                return true;
            }

            if (type == typeof(DateTimeOffset))
            {
                if (reader.CurrentType != IonType.Timestamp) goto NoMatch;
                result = reader.TimestampValue().AsDateTimeOffset();
                return true;
            }


            NoMatch:
            //here means we don't know , try the scalar converter
            return scalarConverter != null && reader.TryConvertTo(type, scalarConverter, out result);
        }
    }
}
