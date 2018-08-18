using System.IO;
using IonDotnet.Conversions;
using IonDotnet.Internals.Binary;

namespace IonDotnet.Serialization
{
    public class IonBinarySerializer
    {
        private readonly ManagedBinaryWriter _binWriter = new ManagedBinaryWriter(BinaryConstants.EmptySymbolTablesArray);

        public byte[] Serialize<T>(T obj, IScalarWriter scalarWriter = null)
        {
            byte[] bytes = null;
            IonSerializationPrivate.WriteObject(_binWriter, obj, scalarWriter);
            _binWriter.Flush(ref bytes);
            _binWriter.Finish();
            return bytes;
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
