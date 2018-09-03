using System.IO;
using IonDotnet.Internals.Binary;

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
                var writer = new ManagedBinaryWriter(stream, BinaryConstants.EmptySymbolTablesArray);
                action(obj, writer);
                writer.Flush();
                writer.Finish();
                //TODO does GetBuffer() returns the correct size?
                var buffer = stream.GetBuffer();
                return buffer.Length == stream.Length ? buffer : stream.ToArray();
            }
        }
    }
}
