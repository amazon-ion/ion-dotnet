using System.Collections.Generic;
using System.IO;
using IonDotnet.Internals.Binary;
using IonDotnet.Utils;

namespace IonDotnet.Builders
{
    public static class IonBinaryWriterBuilder
    {
        /// <summary>
        /// Build a binary writer that write to a stream.
        /// </summary>
        /// <param name="outputStream">Output stream.</param>
        /// <param name="imports">Imported symbol tables used to encode symbols.</param>
        /// <returns>A new Ion writer.</returns>
        public static IIonWriter Build(Stream outputStream, IEnumerable<ISymbolTable> imports = null)
        {
            outputStream.CheckStreamCanWrite();
            return new ManagedBinaryWriter(outputStream, imports ?? Symbols.EmptySymbolTablesArray);
        }
    }
}
