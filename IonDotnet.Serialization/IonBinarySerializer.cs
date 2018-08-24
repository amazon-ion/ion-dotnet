using System;
using System.IO;
using System.Threading.Tasks;
using IonDotnet.Conversions;
using IonDotnet.Internals.Binary;

namespace IonDotnet.Serialization
{
    public class IonBinarySerializer
    {
        public byte[] Serialize<T>(T obj, IScalarWriter scalarWriter = null)
        {
            byte[] bytes = null;
            using (var binWriter = new ManagedBinaryWriter(BinaryConstants.EmptySymbolTablesArray))
            {
                IonSerializationPrivate.WriteObject(binWriter, obj, scalarWriter);
                binWriter.Flush(ref bytes);
                binWriter.Finish();
            }

            return bytes;
        }

        public async Task Serialize<T>(T obj, Stream stream, IScalarWriter scalarWriter = null)
        {
            if (!stream.CanWrite)
                throw new ArgumentException("Stream must be writable", nameof(stream));


            using (var binWriter = new ManagedBinaryWriter(BinaryConstants.EmptySymbolTablesArray))
            {
                IonSerializationPrivate.WriteObject(binWriter, obj, scalarWriter);
                await binWriter.FlushAsync(stream);
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
