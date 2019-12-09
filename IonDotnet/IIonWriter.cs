using System;
using System.Collections.Generic;
using System.Numerics;

// ReSharper disable UnusedMemberInSuper.Global

namespace IonDotnet
{
    /// <summary>
    /// Contains all functions to write to an Ion stream.
    /// </summary>
    public interface IIonWriter : IDisposable
    {
        /// <summary>
        /// Get the current symbol table being used by the writer.
        /// </summary>
        ISymbolTable SymbolTable { get; }

//        /// <summary>
//        /// Flush all the pending written data (including symbol tables) to the output stream asynchronously.
//        /// </summary>
//        /// <returns>The task representing flush operation.</returns>
//        /// <exception cref="System.IO.IOException">I/O error on flushing.</exception>
//        Task FlushAsync();

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

//        /// <summary>
//        /// Mark the end of a datagram, all written values will be flushed (asynchronously). 
//        /// </summary>
//        /// <remarks>
//        /// This method WILL flush the data (including symbol tables) to the output stream. The writer will then be reset to
//        /// the initial state.
//        /// </remarks>
//        /// <exception cref="System.IO.IOException">I/O error on flushing.</exception>
//        Task FinishAsync();

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
        /// Step in a container.
        /// </summary>
        /// <param name="type">Container type</param>
        void StepIn(IonType type);

        /// <summary>
        /// Step out of the current container.
        /// </summary>
        void StepOut();

        /// <summary>
        /// Whether values are being written as fields of a struct
        /// </summary>
        bool IsInStruct { get; }

        /// <summary>
        /// Write the current value from the reader.
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
        /// Set the annotations of the current value.
        /// </summary>
        /// <param name="annotations">Set of annotations.</param>
        void SetTypeAnnotations(IEnumerable<string> annotations);

        /// <summary>
        /// Write a null.null
        /// </summary>
        void WriteNull();

        /// <summary>
        /// Write a <see cref="type" /> null value of a certain type
        /// </summary>
        /// <param name="type"></param>
        void WriteNull(IonType type);

        /// <summary>
        /// Write a boolean value.
        /// </summary>
        /// <param name="value">Boolean value.</param>
        void WriteBool(bool value);

        /// <summary>
        /// Write an integer value.
        /// </summary>
        /// <param name="value">Integer value.</param>
        void WriteInt(long value);

        /// <summary>
        /// Write a big integer value.
        /// </summary>
        /// <param name="value">Big integer value/</param>
        void WriteInt(BigInteger value);

        /// <summary>
        /// Write a floating point value.
        /// </summary>
        /// <param name="value">Floating point value.</param>
        void WriteFloat(double value);

        /// <summary>
        /// Write a decimal value.
        /// </summary>
        /// <param name="value">Decimal value.</param>
        void WriteDecimal(decimal value);

        /// <summary>
        /// Write a <see cref="BigDecimal"/> value.
        /// </summary>
        /// <param name="value">Big decimal value.</param>
        void WriteDecimal(BigDecimal value);

        /// <summary>
        /// Write a timestamp value.
        /// </summary>
        /// <param name="value">Timestamp value.</param>
        void WriteTimestamp(Timestamp value);

        /// <summary>
        /// Write a symbol text.
        /// </summary>
        /// <param name="symbol">Symbol text.</param>
        void WriteSymbol(string symbol);

        /// <summary>
        /// Write a symbol token.
        /// </summary>
        /// <param name="symbolToken">Symbol value.</param>
        void WriteSymbolToken(SymbolToken symbolToken);

        /// <summary>
        /// Write a string value.
        /// </summary>
        /// <param name="value">String value.</param>
        void WriteString(string value);

        /// <summary>
        /// Write a sequence of bytes.
        /// </summary>
        /// <param name="value">Byte buffer.</param>
        void WriteBlob(ReadOnlySpan<byte> value);

        /// <summary>
        /// Write a clob value.
        /// </summary>
        /// <param name="value">Blob value.</param>
        void WriteClob(ReadOnlySpan<byte> value);

        /// <summary>
        /// Add a annotation for the current value.
        /// </summary>
        /// <param name="annotation">The value annotation to add.</param>
        void AddTypeAnnotation(string annotation);

        /// <summary>
        /// Add a type annotation in the form of a symbol. If the text is known, it will be added as is to the current value's annotations.
        /// If the text is unknown (null), the sid will be added to the writer.
        /// </summary>
        /// <param name="annotation">Annotation as symbol token.</param>
        void AddTypeAnnotationSymbol(SymbolToken annotation);

        /// <summary>
        /// Remove all the annotations of the value being written.
        /// </summary>
        void ClearTypeAnnotations();
    }
}
