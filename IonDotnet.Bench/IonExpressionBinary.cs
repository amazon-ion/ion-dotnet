using IonDotnet.Internals.Binary;

namespace IonDotnet.Bench
{
    public static class IonExpressionBinary
    {
        private static readonly ManagedBinaryWriter Writer = new ManagedBinaryWriter(BinaryConstants.EmptySymbolTablesArray);

        
        public static byte[] Serialize<T>(T obj)
        {
            var action = IonSerializerExpression.GetAction<T>();
            // var action = GetAction<T>();
            //now write
            byte[] bytes = null;
            action(obj, Writer);
            Writer.Flush(ref bytes);
            return bytes;
        }
    }
}
