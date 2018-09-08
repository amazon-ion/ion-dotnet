using System;
using System.IO;
using IonDotnet.Internals.Binary;
using IonDotnet.Utils;

namespace IonDotnet.Systems
{
    public static class IonBinaryWriterBuilder
    {
        public static IIonWriter Build(Stream outputStream)
        {
            outputStream.CheckStreamCanWrite();
            return new ManagedBinaryWriter(outputStream, Symbols.EmptySymbolTablesArray);
        }

        public static IIonWriter Build(Stream outputStream, ICatalog catalog)
        {
            outputStream.CheckStreamCanWrite();
            throw new NotImplementedException();
        }
    }
}
