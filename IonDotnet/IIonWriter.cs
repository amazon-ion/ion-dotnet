using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace IonDotnet
{
    /// <inheritdoc cref="IValueWriter" />
    /// <summary>
    /// Contains all functions to write an Ion stream
    /// </summary>
    public interface IIonWriter : IValueWriter, IDisposable
    {
        ISymbolTable SymbolTable { get; }

        /// <summary>
        /// Flushes this writer by writing any buffered output to the underlying output target.
        /// </summary>
        /// <exception cref="System.IO.IOException">When error happens while writing data to output stream</exception>
        void Flush(Stream outputStream);

        void Finish(Stream outputStream = null);

        void SetFieldName(string name);

        void SetFieldNameSymbol(SymbolToken name);

        void StepIn(IonType type);

        void StepOut();

        /// <summary>
        /// Whether values are being written as fields of a struct
        /// </summary>
        bool IsInStruct { get; }

        /// <summary>
        /// Write the current value from the reader
        /// </summary>
        /// <param name="reader">Ion reader</param>
        void WriteValue(IIonReader reader);

        /// <summary>
        /// Writes a reader's current value, and all following values until the end of the current container.
        /// If there's no current value then this method calls {@link IonReader#next()} to get going.
        /// </summary>
        /// <param name="reader">Ion reader</param>
        /// <remarks>This method iterates until <see cref="IIonReader.MoveNext"/> returns null and does not Step out</remarks>
        void WriteValues(IIonReader reader);


        void SetTypeAnnotationSymbols(IEnumerable<SymbolToken> annotations);
    }
}
