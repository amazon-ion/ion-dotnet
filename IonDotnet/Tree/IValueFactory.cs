using System;
using System.Numerics;

namespace IonDotnet.Tree
{
    /// <summary>
    /// The factory for all {@link IonValue}s.
    /// </summary>
    public interface IValueFactory
    {
        /// <summary>
        /// Constructs a new {@code null.blob} instance.
        /// </summary>
        /// <returns>A new {@code null.blob} instance.</returns>
        IIonValue NewNullBlob();

        /// <summary>
        /// Constructs a new Ion {@code blob} instance, copying bytes from an array.
        /// </summary>
        /// <param name="bytes">The data for the new blob.</param>
        /// <returns>A new Ion {@code blob} instance.</returns>
        IIonValue NewBlob(ReadOnlySpan<byte> bytes);

        /// <summary>
        /// Constructs a new {@code null.bool} instance.
        /// </summary>
        /// <returns>A new {@code null.bool} instance.</returns>
        IIonValue NewNullBool();

        /// <summary>
        /// Constructs a new {@code bool} instance with the given value.
        /// </summary>
        /// <param name="value">The new {@code bool}'s value.</param>
        /// <returns>A {@code bool} initialized with the provided value.</returns>
        IIonValue NewBool(bool value);

        /// <summary>
        /// Constructs a new {@code null.clob} instance.
        /// </summary>
        /// <returns>A new {@code null.clob} instance.</returns>
        IIonValue NewNullClob();

        /// <summary>
        /// Constructs a new Ion {@code clob} instance, copying bytes from an array.
        /// </summary>
        /// <param name="bytes">The data for the new clob.</param>
        /// <returns>A new Ion {@code clob} instance.</returns>
        IIonValue NewClob(ReadOnlySpan<byte> bytes);

        /// <summary>
        /// Constructs a new {@code null.decimal} instance.
        /// </summary>
        /// <returns>A new {@code null.decimal} instance.</returns>
        IIonValue NewNullDecimal();

        /// <summary>
        /// Constructs a new Ion {@code decimal} instance from a C# {@code double}.
        /// </summary>
        /// <param name="doubleValue">The value for the new decimal.</param>
        /// <returns>A new Ion {@code decimal} instance.</returns>
        IIonValue NewDecimal(double doubleValue);

        /// <summary>
        /// Constructs a new Ion {@code decimal} instance from a C# {@code decimal}.
        /// </summary>
        /// <param name="value">The value for the new decimal.</param>
        /// <returns>A new Ion {@code decimal} instance.</returns>
        IIonValue NewDecimal(decimal value);

        /// <summary>
        /// Constructs a new Ion {@code decimal} instance from a C# {@code BigDecimal}.
        /// </summary>
        /// <param name="bigDecimal">The value for the new decimal.</param>
        /// <returns>A new Ion {@code decimal} instance.</returns>
        IIonValue NewDecimal(BigDecimal bigDecimal);

        /// <summary>
        /// Constructs a new {@code null.float} instance.
        /// </summary>
        /// <returns>A new {@code null.float} instance.</returns>
        IIonValue NewNullFloat();

        /// <summary>
        /// Constructs a new Ion {@code float} instance from a C# {@code double}.
        /// </summary>
        /// <param name="value">The value for the new float.</param>
        /// <returns>A new Ion {@code float} instance.</returns>
        IIonValue NewFloat(double value);

        /// <summary>
        /// Constructs a new {@code null.int} instance.
        /// </summary>
        /// <returns>A new {@code null.int} instance.</returns>
        IIonValue NewNullInt();

        /// <summary>
        /// Constructs a new {@code int} instance with the given value.
        /// </summary>
        /// <param name="value">The value for the new int.</param>
        /// <returns>A new {@code int} instance.</returns>
        IIonValue NewInt(long value);

        /// <summary>
        /// Constructs a new {@code int} instance with the given value.
        /// </summary>
        /// <param name="value">The value for the new int.</param>
        /// <returns>A new {@code int} instance.</returns>
        IIonValue NewInt(BigInteger value);

        /// <summary>
        /// Constructs a new {@code null.list} instance.
        /// </summary>
        /// <returns>A new {@code null.list} instance.</returns>
        IIonValue NewNullList();

        /// <summary>
        /// Constructs a new empty (not null) {@code list} instance.
        /// </summary>
        /// <returns>A new empty {@code list} instance.</returns>
        IIonValue NewEmptyList();

        /// <summary>
        /// Constructs a new {@code null.null} instance.
        /// </summary>
        /// <returns>A new {@code null.null} instance.</returns>
        IIonValue NewNull();

        /// <summary>
        /// Constructs a new {@code null.sexp} instance.
        /// </summary>
        /// <returns>A new {@code null.sexp} instance.</returns>
        IIonValue NewNullSexp();

        /// <summary>
        /// Constructs a new empty (not null) {@code sexp} instance.
        /// </summary>
        /// <returns>A new empty {@code sexp} instance.</returns>
        IIonValue NewEmptySexp();

        /// <summary>
        /// Constructs a new {@code null.string} instance.
        /// </summary>
        /// <returns>A new {@code null.string} instance.</returns>
        IIonValue NewNullString();

        /// <summary>
        /// Constructs a new Ion string with the given value.
        /// </summary>
        /// <param name="value">The value of the text for the new string.</param>
        /// <returns>A new {@code string} instance.</returns>
        IIonValue NewString(string value);

        /// <summary>
        /// Constructs a new {@code null.struct} instance.
        /// </summary>
        /// <returns>A new {@code null.struct} instance.</returns>
        IIonValue NewNullStruct();

        /// <summary>
        /// Constructs a new empty (not null) {@code struct} instance.
        /// </summary>
        /// <returns>A new empty {@code struct} instance.</returns>
        IIonValue NewEmptyStruct();

        /// <summary>
        /// Constructs a new {@code null.symbol} instance.
        /// </summary>
        /// <returns>A new {@code null.symbol} instance.</returns>
        IIonValue NewNullSymbol();

        /// <summary>
        /// Constructs a new Ion symbol with the given symbol token.
        /// </summary>
        /// <param name="symbolToken">The value the text and/or SID of the symbol.</param>
        /// <returns>A new {@code symbol} instance.</returns>
        IIonValue NewSymbol(SymbolToken symbolToken);

        /// <summary>
        /// Constructs a new Ion symbol with the given values.
        /// </summary>
        /// <param name="text">The text value for the new symbol.</param>
        /// <returns>A new {@code symbol} instance.</returns>
        IIonValue NewSymbol(string text);

        /// <summary>
        /// Constructs a new {@code null.timestamp} instance.
        /// </summary>
        /// <returns>A new {@code null.timestamp} instance.</returns>
        IIonValue NewNullTimestamp();

        /// <summary>
        /// Constructs a new {@code timestamp} instance with the given value.
        /// </summary>
        /// <param name="val">The value for the new timestamp.</param>
        /// <returns>A new {@code timestamp} instance.</returns>
        IIonValue NewTimestamp(Timestamp val);
    }
}
