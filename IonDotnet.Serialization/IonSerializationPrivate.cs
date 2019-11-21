using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace IonDotnet.Serialization
{
    internal static partial class IonSerializationPrivate
    {
        private static readonly IDictionary<Type, PropertyInfo[]> PropertyInfoMap = new Dictionary<Type, PropertyInfo[]>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static PropertyInfo[] GetPublicProperties(Type type)
        {
            if (PropertyInfoMap.TryGetValue(type, out var props)) return props;

            props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            PropertyInfoMap.Add(type, props);
            return props;
        }

        /// <summary>
        /// Write the object to the writer, don't care the level/container, don't know the type
        /// </summary>
        internal static void WriteObject(IIonWriter writer, object obj, IScalarWriter scalarWriter)
        {
            if (obj == null)
            {
                writer.WriteNull();
                return;
            }

            WriteObject(writer, obj, obj.GetType(), scalarWriter);
        }

        /// <summary>
        /// Write object knowing the (intended) type
        /// </summary>
        private static void WriteObject(IIonWriter writer, object obj, Type type, IScalarWriter scalarWriter)
        {
            Debug.Assert(obj == null || obj.GetType() == type, $"objType: {obj?.GetType()}, type:{type}");

            if (TryWriteScalar(writer, obj, type, scalarWriter)) return;

            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                //special case of byte[]
                if (TryWriteByteArray(writer, obj, type)) return;

                //is this a null.list?
                if (obj == null)
                {
                    writer.WriteNull(IonType.List);
                    return;
                }

                //Write everything to list now
                var enumerable = (IEnumerable) obj;
                writer.StepIn(IonType.List);
                foreach (var item in enumerable)
                {
                    WriteObject(writer, item, scalarWriter);
                }

                writer.StepOut();
                return;
            }

            //not scalar, not list, must be a struct
            if (obj == null)
            {
                writer.WriteNull();
                return;
            }

            writer.StepIn(IonType.Struct);

            var properties = GetPublicProperties(type);
            foreach (var propertyInfo in properties)
            {
                if (!propertyInfo.CanRead) continue;
                //with property, we have to be careful because they have type declaration which might carry intention
                //for 
                writer.SetFieldName(propertyInfo.Name);
                WriteObject(writer, propertyInfo.GetValue(obj), propertyInfo.PropertyType, scalarWriter);
            }

            writer.StepOut();
        }

        private static bool TryWriteByteArray(IIonWriter writer, object obj, Type type)
        {
            if (obj == null)
            {
                if (!typeof(byte[]).IsAssignableFrom(type) && !typeof(IEnumerable<byte>).IsAssignableFrom(type)) return false;

                writer.WriteNull(IonType.Blob);
                return true;
            }

            if (type == typeof(byte[]))
            {
                //fast path, just check directly for byte array
                writer.WriteBlob((byte[]) obj);
                return true;
            }

            if (type == typeof(ReadOnlyMemory<byte>))
            {
                //fast path 2
                writer.WriteBlob(((ReadOnlyMemory<byte>) obj).Span);
                return true;
            }

            if (type == typeof(Memory<byte>))
            {
                //fast path 3
                writer.WriteBlob(((Memory<byte>) obj).Span);
                return true;
            }

            //slow path, does anyone really use List<byte> ???
            if (!(obj is IEnumerable<byte> enumerable)) return false;

            writer.WriteBlob(enumerable.ToArray());
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryWriteScalar(IIonWriter writer, object obj, Type type, IScalarWriter scalarWriter)
        {
            if (type.IsEnum)
            {
                var propValue = Enum.GetName(type, obj);
                writer.WriteSymbol(propValue);
                return true;
            }

            if (type == typeof(string))
            {
                var propValue = (string) obj;
                writer.WriteString(propValue);
                return true;
            }

            if (type == typeof(int))
            {
                var propValue = (int) obj;
                writer.WriteInt(propValue);
                return true;
            }

            if (type == typeof(long))
            {
                var propValue = (long) obj;
                writer.WriteInt(propValue);
                return true;
            }

            if (type == typeof(bool))
            {
                var propValue = (bool) obj;
                writer.WriteBool(propValue);
                return true;
            }

            if (type == typeof(float))
            {
                var propValue = (float) obj;
                writer.WriteFloat(propValue);
                return true;
            }

            if (type == typeof(double))
            {
                var propValue = (double) obj;
                writer.WriteFloat(propValue);
                return true;
            }

            if (type == typeof(DateTime))
            {
                var propValue = (DateTime) obj;
                writer.WriteTimestamp(new Timestamp(propValue));
                return true;
            }

            if (type == typeof(DateTimeOffset))
            {
                var propValue = (DateTimeOffset) obj;
                writer.WriteTimestamp(new Timestamp(propValue));
                return true;
            }

            if (type == typeof(decimal))
            {
                var propValue = (decimal) obj;
                writer.WriteDecimal(propValue);
                return true;
            }

            //try to see if we can write use the scalar writer
            if (scalarWriter != null)
            {
                var method = scalarWriter.GetType().GetMethod(nameof(IScalarWriter.TryWriteValue));
                var genericMethod = method.MakeGenericMethod(type);
                return (bool) genericMethod.Invoke(scalarWriter, new[] {writer, obj});
            }

            return false;
        }
    }
}
