using System;
using System.IO;
using System.Threading.Tasks;
using IonDotnet.Conversions;
using IonDotnet.Internals.Text;
using IonDotnet.Systems;

namespace IonDotnet.Serialization
{
    public class IonTextSerializer
    {
        public string Serialize<T>(T obj, IScalarWriter scalarWriter = null)
            => Serialize(obj, IonTextOptions.Default, scalarWriter);

        public string Serialize<T>(T obj, IonTextOptions options, IScalarWriter scalarWriter = null)
        {
            using (var sw = new StringWriter())
            {
                var writer = new IonTextWriter(sw, options);
                IonSerializationPrivate.WriteObject(writer, obj, scalarWriter);
                return sw.ToString();
            }
        }

        public Task SerializeAsync<T>(T obj, Stream stream, IonTextOptions options)
        {
            if (!stream.CanWrite)
                throw new ArgumentException("Stream must be writable", nameof(stream));

            using (var streamWriter = new StreamWriter(stream))
            {
                var writer = new IonTextWriter(streamWriter, options);
                IonSerializationPrivate.WriteObject(writer, obj, null);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Deserialize a text format to object type T
        /// </summary>
        /// <param name="text">Text input</param>
        /// <param name="scalarConverter"></param>
        /// <typeparam name="T">Type of object to deserialize to</typeparam>
        /// <returns>Deserialized object</returns>
        public T Deserialize<T>(string text, IScalarConverter scalarConverter = null)
        {
            var reader = new UserTextReader(text);
            reader.MoveNext();
            return (T) IonSerializationPrivate.Deserialize(reader, typeof(T), scalarConverter);
        }
    }
}
