using System.IO;
using IonDotnet.Internals.Binary;
using IonDotnet.Utils;

namespace IonDotnet.Bench
{
    public static class IonExpressionBinary
    {
        public static byte[] Serialize<T>(T obj)
        {
            var action = IonSerializerExpression.GetAction<T>();

            // var action = GetAction<T>();
            //now write
            using (var stream = new MemoryStream())
            {
                var writer = new ManagedBinaryWriter(stream, Symbols.EmptySymbolTablesArray);
                action(obj, writer);
                writer.Flush();
                writer.Finish();
                return stream.GetWrittenBuffer();
            }
        }
    }
}
