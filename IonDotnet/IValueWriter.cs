using System;
using System.Numerics;

namespace IonDotnet
{
    /// <summary>
    /// Represents an interface that can write value to an Ion stream
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
        /// Write a boolean value to the stream
        /// </summary>
        /// <param name="value">The value to write</param>
        void WriteBool(bool value);

        void WriteInt(long value);

        void WriteInt(BigInteger value);

        void WriteFloat(double value);

        void WriteDecimal(decimal value);

        void WriteTimestamp(Timestamp value);

        void WriteSymbol(string symbol);

        void WriteSymbolToken(SymbolToken symbolToken);

        void WriteString(string value);

        void WriteBlob(ReadOnlySpan<byte> value);

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
