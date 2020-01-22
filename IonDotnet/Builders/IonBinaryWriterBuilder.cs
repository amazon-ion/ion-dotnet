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
        /// <param name="forceFloat64">Always write float values in 64 bits. When false, float values will be
        /// written in 32 bits when it is possible to do so without losing fidelity.</param>
        /// <returns>A new Ion writer.</returns>
        public static IIonWriter Build(
            Stream outputStream,
            IEnumerable<ISymbolTable> imports = null,
            bool forceFloat64 = false)
        {
            outputStream.CheckStreamCanWrite();
            return new ManagedBinaryWriter(
                outputStream,
                imports ?? Symbols.EmptySymbolTablesArray,
                forceFloat64);
        }
    }
}
