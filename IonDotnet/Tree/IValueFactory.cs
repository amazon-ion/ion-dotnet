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
        IIonBlob NewNullBlob();

        /// <summary>
        /// Constructs a new Ion {@code blob} instance, copying bytes from an array.
        /// </summary>
        /// <param name="bytes">The data for the new blob.</param>
        /// <returns>A new Ion {@code blob} instance.</returns>
        IIonBlob NewBlob(ReadOnlySpan<byte> bytes);

        /// <summary>
        /// Constructs a new {@code null.bool} instance.
        /// </summary>
        /// <returns>A new {@code null.bool} instance.</returns>
        IIonBool NewNullBool();

        /// <summary>
        /// Constructs a new {@code bool} instance with the given value.
        /// </summary>
        /// <param name="value">The new {@code bool}'s value.</param>
        /// <returns>A {@code bool} initialized with the provided value.</returns>
        IIonBool NewBool(bool value);

        /// <summary>
        /// Constructs a new {@code null.clob} instance.
        /// </summary>
        /// <returns>A new {@code null.clob} instance.</returns>
        IIonClob NewNullClob();

        /// <summary>
        /// Constructs a new Ion {@code clob} instance, copying bytes from an array.
        /// </summary>
        /// <param name="bytes">The data for the new clob.</param>
        /// <returns>A new Ion {@code clob} instance.</returns>
        IIonClob NewClob(ReadOnlySpan<byte> bytes);

        /// <summary>
        /// Constructs a new {@code null.decimal} instance.
        /// </summary>
        /// <returns>A new {@code null.decimal} instance.</returns>
        IIonDecimal NewNullDecimal();

        /// <summary>
        /// Constructs a new Ion {@code decimal} instance from a C# {@code double}.
        /// </summary>
        /// <param name="doubleValue">The value for the new decimal.</param>
        /// <returns>A new Ion {@code decimal} instance.</returns>
        IIonDecimal NewDecimal(double doubleValue);

        /// <summary>
        /// Constructs a new Ion {@code decimal} instance from a C# {@code decimal}.
        /// </summary>
        /// <param name="value">The value for the new decimal.</param>
        /// <returns>A new Ion {@code decimal} instance.</returns>
        IIonDecimal NewDecimal(decimal value);

        /// <summary>
        /// Constructs a new Ion {@code decimal} instance from a C# {@code BigDecimal}.
        /// </summary>
        /// <param name="bigDecimal">The value for the new decimal.</param>
        /// <returns>A new Ion {@code decimal} instance.</returns>
        IIonDecimal NewDecimal(BigDecimal bigDecimal);

        /// <summary>
        /// Constructs a new {@code null.float} instance.
        /// </summary>
        /// <returns>A new {@code null.float} instance.</returns>
        IIonFloat NewNullFloat();

        /// <summary>
        /// Constructs a new Ion {@code float} instance from a C# {@code double}.
        /// </summary>
        /// <param name="value">The value for the new float.</param>
        /// <returns>A new Ion {@code float} instance.</returns>
        IIonFloat NewFloat(double value);

        /// <summary>
        /// Constructs a new {@code null.int} instance.
        /// </summary>
        /// <returns>A new {@code null.int} instance.</returns>
        IIonInt NewNullInt();

        /// <summary>
        /// Constructs a new {@code int} instance with the given value.
        /// </summary>
        /// <param name="value">The value for the new int.</param>
        /// <returns>A new {@code int} instance.</returns>
        IIonInt NewInt(long value);

        /// <summary>
        /// Constructs a new {@code int} instance with the given value.
        /// </summary>
        /// <param name="value">The value for the new int.</param>
        /// <returns>A new {@code int} instance.</returns>
        IIonInt NewInt(BigInteger value);

        /// <summary>
        /// Constructs a new {@code null.list} instance.
        /// </summary>
        /// <returns>A new {@code null.list} instance.</returns>
        IIonList NewNullList();

        /// <summary>
        /// Constructs a new empty (not null) {@code list} instance.
        /// </summary>
        /// <returns>A new empty {@code list} instance.</returns>
        IIonList NewEmptyList();

        /// <summary>
        /// Constructs a new {@code null.null} instance.
        /// </summary>
        /// <returns>A new {@code null.null} instance.</returns>
        IIonNull NewNull();

        /// <summary>
        /// Constructs a new {@code null.sexp} instance.
        /// </summary>
        /// <returns>A new {@code null.sexp} instance.</returns>
        IIonSexp NewNullSexp();

        /// <summary>
        /// Constructs a new empty (not null) {@code sexp} instance.
        /// </summary>
        /// <returns>A new empty {@code sexp} instance.</returns>
        IIonSexp NewEmptySexp();

        /// <summary>
        /// Constructs a new {@code null.string} instance.
        /// </summary>
        /// <returns>A new {@code null.string} instance.</returns>
        IIonString NewNullString();

        /// <summary>
        /// Constructs a new Ion string with the given value.
        /// </summary>
        /// <param name="value">The value of the text for the new string.</param>
        /// <returns>A new {@code string} instance.</returns>
        IIonString NewString(string value);

        /// <summary>
        /// Constructs a new {@code null.struct} instance.
        /// </summary>
        /// <returns>A new {@code null.struct} instance.</returns>
        IIonStruct NewNullStruct();

        /// <summary>
        /// Constructs a new empty (not null) {@code struct} instance.
        /// </summary>
        /// <returns>A new empty {@code struct} instance.</returns>
        IIonStruct NewEmptyStruct();

        /// <summary>
        /// Constructs a new {@code null.symbol} instance.
        /// </summary>
        /// <returns>A new {@code null.symbol} instance.</returns>
        IIonSymbol NewNullSymbol();

        /// <summary>
        /// Constructs a new Ion symbol with the given symbol token.
        /// </summary>
        /// <param name="symbolToken">The value the text and/or SID of the symbol.</param>
        /// <returns>A new {@code symbol} instance.</returns>
        IIonSymbol NewSymbol(SymbolToken symbolToken);

        /// <summary>
        /// Constructs a new Ion symbol with the given values.
        /// </summary>
        /// <param name="text">The text value for the new symbol.</param>
        /// <returns>A new {@code symbol} instance.</returns>
        IIonSymbol NewSymbol(string text);

        /// <summary>
        /// Constructs a new {@code null.timestamp} instance.
        /// </summary>
        /// <returns>A new {@code null.timestamp} instance.</returns>
        IIonTimestamp NewNullTimestamp();

        /// <summary>
        /// Constructs a new {@code timestamp} instance with the given value.
        /// </summary>
        /// <param name="val">The value for the new timestamp.</param>
        /// <returns>A new {@code timestamp} instance.</returns>
        IIonTimestamp NewTimestamp(Timestamp val);
    }
}
