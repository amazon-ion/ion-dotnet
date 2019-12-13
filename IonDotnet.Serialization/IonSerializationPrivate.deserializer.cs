using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;

namespace IonDotnet.Serialization
{
    /// <summary>
    /// Deserializer
    /// </summary>
    internal static partial class IonSerializationPrivate
    {
        internal static object Deserialize(IIonReader reader, Type type)
        {
            object t = null;

            if (TryDeserializeScalar(reader, type, ref t)) return t;
            if (TryDeserializeCollection(reader, type, ref t)) return t;

            //object
            t = Activator.CreateInstance(type);
            reader.StepIn();

            while (reader.MoveNext() != null)
            {
                //find the property
                var prop = type.GetProperty(reader.CurrentFieldName, BindingFlags.Public | BindingFlags.Instance);
                if (prop == null) continue;
                if (!prop.CanWrite) throw new IonException($"Property {type.Name}.{prop.Name} cannot be set");

                var propValue = Deserialize(reader, prop.PropertyType);
                prop.SetValue(t, propValue);
            }

            reader.StepOut();
            return t;
        }

        private static bool TryDeserializeCollection(IIonReader reader, Type type, ref object result)
        {
            if (!typeof(IEnumerable).IsAssignableFrom(type)) return false;

            //special case of byte array
            if (TryDeserializeByteArray(reader, type, ref result)) return true;

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

            while (reader.MoveNext() != null)
            {
                var element = Deserialize(reader, elementType);
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

        private static bool TryDeserializeByteArray(IIonReader reader, Type type, ref object result)
        {
            if (!type.IsAssignableFrom(typeof(byte[]))) return false;
            if (reader.CurrentType != IonType.Blob && reader.CurrentType != IonType.Clob) return false;

            var bytes = new byte[reader.GetLobByteSize()];
            reader.GetBytes(bytes);
            result = bytes;
            return true;
        }

        private static bool TryDeserializeScalar(IIonReader reader, Type type, ref object result)
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
                result = reader.DecimalValue().ToDecimal();
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
            return false;
        }
    }
}
