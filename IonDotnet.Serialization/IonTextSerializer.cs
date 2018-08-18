using System.IO;
using IonDotnet.Conversions;
using IonDotnet.Internals.Binary;
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
            var sw = new StringWriter();
            var writer = new IonTextWriter(sw, options);
            IonSerializationPrivate.WriteObject(writer, obj, scalarWriter);
            return sw.ToString();
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
