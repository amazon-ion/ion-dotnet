using System.IO;
using IonDotnet.Internals.Text;

namespace IonDotnet.Bench
{
    public static class IonExpressionText
    {
        public static string Serialize<T>(T obj)
        {
            var sw = new StringWriter();
            var writer = new IonTextWriter(sw);
            var action = IonSerializerExpression.GetAction<T>();
            action(obj, writer);
            writer.Finish();
            return sw.ToString();
        }
    }
}
