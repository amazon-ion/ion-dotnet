using System;
using System.Numerics;

// ReSharper disable UnusedMemberInSuper.Global

namespace IonDotnet
{
    /// <summary>
    /// Represents an interface that can write value to an Ion stream.
    /// </summary>
    public interface IValueWriter
    {
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
        /// Set the annotation for the current value.
        /// </summary>
        /// <param name="annotation">The value annotation</param>
        /// <remarks>This will erase all existing annotations for the current value.</remarks>
        void SetTypeAnnotation(string annotation);

        /// <summary>
        /// Add a annotation for the current value.
        /// </summary>
        /// <param name="annotation">The value annotation to add.</param>
        void AddTypeAnnotation(string annotation);
    }
}
