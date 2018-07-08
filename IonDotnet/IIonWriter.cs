using System;
using System.Numerics;

namespace IonDotnet
{
    public interface IIonWriter : IDisposable
    {
        ISymbolTable SymbolTable { get; }

        /// <summary>
        /// Flushes this writer by writing any buffered output to the underlying output target
        /// </summary>
        /// <exception cref="System.IO.IOException"></exception>
        void Flush();

        void Finish();

        void Close();

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
        /// <remarks>This method iterates until <see cref="IIonReader.Next"/> returns null and does not Step out</remarks>
        void WriteValues(IIonReader reader);

        /// <summary>
        /// Write a null.null
        /// </summary>
        void WriteNull();

        /// <summary>
        /// Write a <see cref="type" /> null
        /// </summary>
        /// <param name="type"></param>
        void WriteNull(IonType type);

        void WriteBool(bool value);

        void WriteInt(long value);

        void WriteInt(BigInteger value);

        void WriteFloat(double value);

        void WriteDecimal(decimal value);

        void WriteTimestamp(DateTime value);

        void WriteSymbol(SymbolToken symbolToken);

        void WriteString(string value);

        void WriteBlob(byte[] value);

        void WriteBlob(ArraySegment<byte> value);

        void WriteClob(byte[] value);

        void WriteClob(ArraySegment<byte> value);

        void SetTypeAnnotations(params string[] annotations);

        void SetTypeAnnotationSymbols(ArraySegment<SymbolToken> annotations);

        void AddTypeAnnotation(string annotation);
    }
}
