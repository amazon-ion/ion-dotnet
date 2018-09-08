using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        /// Flush all the pending written data (including symbol tables) to the output stream asynchronously.
        /// </summary>
        /// <returns>The task representing flush operation.</returns>
        /// <exception cref="System.IO.IOException">I/O error on flushing.</exception>
        Task FlushAsync();

        /// <summary>
        /// Flush all the pending written data (including symbol tables) to the output stream (blocking).
        /// </summary>
        /// <exception cref="System.IO.IOException">I/O error on flushing.</exception>
        void Flush();

        /// <summary>
        /// Mark the end of a datagram, all written values will be flushed (blocking). 
        /// </summary>
        /// <remarks>
        /// This method WILL flush the data (including symbol tables) to the output stream. The writer will then be reset to
        /// the initial state.
        /// </remarks>
        /// <exception cref="System.IO.IOException">I/O error on flushing.</exception>
        void Finish();

        /// <summary>
        /// Mark the end of a datagram, all written values will be flushed (asynchronously). 
        /// </summary>
        /// <remarks>
        /// This method WILL flush the data (including symbol tables) to the output stream. The writer will then be reset to
        /// the initial state.
        /// </remarks>
        /// <exception cref="System.IO.IOException">I/O error on flushing.</exception>
        Task FinishAsync();

        /// <summary>
        /// Set the field name, must be called when in a Struct
        /// </summary>
        /// <param name="name">Field name</param>
        void SetFieldName(string name);

        /// <summary>
        /// Set the field name, but as a <see cref="SymbolToken"/>
        /// </summary>
        /// <param name="symbol">Symbol token</param>
        void SetFieldNameSymbol(SymbolToken symbol);

        /// <summary>
        /// Step in a container
        /// </summary>
        /// <param name="type">Container type</param>
        void StepIn(IonType type);

        /// <summary>
        /// Step out of the current container
        /// </summary>
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

        /// <summary>
        /// Set the annotations of the current value
        /// </summary>
        /// <param name="annotations">Set of annotations</param>
        void SetTypeAnnotationSymbols(IEnumerable<SymbolToken> annotations);
    }
}
