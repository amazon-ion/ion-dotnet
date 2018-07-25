using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using IonDotnet.Internals;
using IonDotnet.Internals.Binary;

namespace IonDotnet.Serialization
{
    public class IonSerializer
    {
        public byte[] Serialize(object obj)
        {
            //prepare infrastructure
            var stream = new MemoryStream();
            using (var writer = new ManagedBinaryWriter(stream, IonConstants.EmptySymbolTablesArray))
            {
                WriteObject(writer, obj);
                writer.Finish();
            }

            return stream.ToArray();
        }

        /// <summary>
        /// Write the object to the writer, don't care the level/container, don't know the type
        /// </summary>
        private void WriteObject(IIonWriter writer, object obj)
        {
            if (obj == null)
            {
                writer.WriteNull();
                return;
            }

            WriteObject(writer, obj, obj.GetType());
        }

        /// <summary>
        /// Write object knowing the (intended) type
        /// </summary>
        private void WriteObject(IIonWriter writer, object obj, Type type)
        {
            Debug.Assert(obj == null || obj.GetType() == type);

            if (TryWriteScalar(writer, obj, type)) return;

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
                //TODO do we enforce generic type?
                var enumerable = (IEnumerable) obj;
                writer.StepIn(IonType.List);
                foreach (var item in enumerable)
                {
                    WriteObject(writer, item);
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

            var properties = obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var propertyInfo in properties)
            {
                if (!propertyInfo.CanRead) continue;
                //with property, we have to be careful because they have type declaration which might carry intention
                //for 
                writer.SetFieldName(propertyInfo.Name);
                WriteObject(writer, propertyInfo.GetValue(obj), propertyInfo.PropertyType);
            }

            writer.StepOut();
        }

        private static bool TryWriteByteArray(IIonWriter writer, object obj, Type type)
        {
            if (obj is byte[] byteArray)
            {
                //fast path, just check directly for byte array
                writer.WriteBlob(byteArray);
                return true;
            }

            if (obj is ReadOnlyMemory<byte> readOnlyMemorymemory)
            {
                //fast path 2
                writer.WriteBlob(readOnlyMemorymemory.Span);
                return true;
            }

            if (obj is Memory<byte> memory)
            {
                //fast path 3
                writer.WriteBlob(memory.Span);
                return true;
            }

            if (obj == null)
            {
                if (!typeof(byte[]).IsAssignableFrom(type) && !typeof(IEnumerable<byte>).IsAssignableFrom(type)) return false;

                writer.WriteNull(IonType.Blob);
                return true;
            }

            //slow path, does anyone really use List<byte> ???
            if (!(obj is IEnumerable<byte> enumerable)) return false;

            writer.WriteBlob(enumerable.ToArray());
            return true;
        }

        private static bool TryWriteScalar(IIonWriter writer, object obj, Type type)
        {
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
                writer.WriteTimestamp(propValue);
                return true;
            }

            if (type == typeof(decimal))
            {
                var propValue = (decimal) obj;
                writer.WriteDecimal(propValue);
                return true;
            }

            return false;
        }
    }
}
