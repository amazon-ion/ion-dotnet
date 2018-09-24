using System;
using System.IO;
using IonDotnet.Conversions;
using IonDotnet.Internals.Binary;
using IonDotnet.Utils;

namespace IonDotnet.Serialization
{
    public class IonBinarySerializer
    {
        public byte[] Serialize<T>(T obj, IScalarWriter scalarWriter = null)
        {
            using (var stream = new MemoryStream())
            {
                using (var binWriter = new ManagedBinaryWriter(stream, Symbols.EmptySymbolTablesArray))
                {
                    IonSerializationPrivate.WriteObject(binWriter, obj, scalarWriter);
                    binWriter.Flush();
                    binWriter.Finish();
                }

                return stream.GetWrittenBuffer();
            }
        }

        public void Serialize<T>(T obj, Stream stream, IScalarWriter scalarWriter = null)
        {
            if (!stream.CanWrite)
                throw new ArgumentException("Stream must be writable", nameof(stream));


            using (var binWriter = new ManagedBinaryWriter(stream, Symbols.EmptySymbolTablesArray))
            {
                IonSerializationPrivate.WriteObject(binWriter, obj, scalarWriter);
                binWriter.Flush();
            }
        }

        /// <summary>
        /// Deserialize a binary format to object type T
        /// </summary>
        /// <param name="binary">Binary input</param>
        /// <param name="scalarConverter"></param>
        /// <typeparam name="T">Type of object to deserialize to</typeparam>
        /// <returns>Deserialized object</returns>
        public T Deserialize<T>(byte[] binary, IScalarConverter scalarConverter = null)
        {
            using (var stream = new MemoryStream(binary))
            {
                var reader = new UserBinaryReader(stream, scalarConverter);
                reader.MoveNext();
                return (T) IonSerializationPrivate.Deserialize(reader, typeof(T), scalarConverter);
            }
        }
    }
}
