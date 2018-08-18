using System.IO;
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
    }
}
