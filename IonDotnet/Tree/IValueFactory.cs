using System;
using System.Numerics;
using IonDotnet.Tree.Impl;

namespace IonDotnet.Tree
{
    /// <summary>
    /// The factory for all {@link IonValue}s.
    /// WARNING: This interface should not be implemented or extended by
    /// code outside of this library.
    /// </summary>
    public interface IValueFactory
    {
        /// <summary>
        /// Constructs a new {@code null.blob} instance.
        /// </summary>
        /// <returns>A new {@code null.blob} instance.</returns>
        IonBlob NewNullBlob();

        /// <summary>
        /// Constructs a new Ion {@code blob} instance, copying bytes from an array.
        /// </summary>
        /// <param name="bytes">The data for the new blob.</param>
        /// <returns>A new Ion {@code blob} instance.</returns>
        IonBlob NewBlob(ReadOnlySpan<byte> bytes);

        /// <summary>
        /// Constructs a new {@code null.bool} instance.
        /// </summary>
        /// <returns>A new {@code null.bool} instance.</returns>
        IonBool NewNullBool();

        /// <summary>
        /// Constructs a new {@code bool} instance with the given value.
        /// </summary>
        /// <param name="value">The new {@code bool}'s value.</param>
        /// <returns>A {@code bool} initialized with the provided value.</returns>
        IonBool NewBool(bool value);

        /// <summary>
        /// Constructs a new {@code null.clob} instance.
        /// </summary>
        /// <returns>A new {@code null.clob} instance.</returns>
        IonClob NullClob();

        /// <summary>
        /// Constructs a new Ion {@code clob} instance, copying bytes from an array.
        /// </summary>
        /// <param name="bytes">The data for the new clob.</param>
        /// <returns>A new Ion {@code clob} instance.</returns>
        IonClob NewClob(ReadOnlySpan<byte> bytes);

        /// <summary>
        /// Constructs a new {@code null.decimal} instance.
        /// </summary>
        /// <returns>A new {@code null.decimal} instance.</returns>
        IonDecimal NewNullDecimal();

        /// <summary>
        /// Constructs a new Ion {@code decimal} instance from a C# {@code double}.
        /// </summary>
        /// <param name="doubleValue">The value for the new decimal.</param>
        /// <returns>A new Ion {@code decimal} instance.</returns>
        IonDecimal NewDecimal(double doubleValue);

        /// <summary>
        /// Constructs a new Ion {@code decimal} instance from a C# {@code decimal}.
        /// </summary>
        /// <param name="value">The value for the new decimal.</param>
        /// <returns>A new Ion {@code decimal} instance.</returns>
        IonDecimal NewDecimal(decimal value);

        /// <summary>
        /// Constructs a new Ion {@code decimal} instance from a C# {@code BigDecimal}.
        /// </summary>
        /// <param name="bigDecimal">The value for the new decimal.</param>
        /// <returns>A new Ion {@code decimal} instance.</returns>
        IonDecimal NewDecimal(BigDecimal bigDecimal);

        /// <summary>
        /// Constructs a new {@code null.float} instance.
        /// </summary>
        /// <returns>A new {@code null.float} instance.</returns>
        IonFloat NewNullFloat();

        /// <summary>
        /// Constructs a new Ion {@code float} instance from a C# {@code double}.
        /// </summary>
        /// <param name="value">The value for the new float.</param>
        /// <returns>A new Ion {@code float} instance.</returns>
        IonFloat NewFloat(double value);

        /// <summary>
        /// Constructs a new {@code null.int} instance.
        /// </summary>
        /// <returns>A new {@code null.int} instance.</returns>
        IonInt NewNullInt();

        /// <summary>
        /// Constructs a new {@code int} instance with the given value.
        /// </summary>
        /// <param name="value">The value for the new int.</param>
        /// <returns>A new {@code int} instance.</returns>
        IonInt NewInt(long value);

        /// <summary>
        /// Constructs a new {@code int} instance with the given value.
        /// </summary>
        /// <param name="value">The value for the new int.</param>
        /// <returns>A new {@code int} instance.</returns>
        IonInt NewInt(BigInteger value);

        /// <summary>
        /// Constructs a new {@code null.list} instance.
        /// </summary>
        /// <returns>A new {@code null.list} instance.</returns>
        IonList NewNullList();

        /// <summary>
        /// Constructs a new empty (not null) {@code list} instance.
        /// </summary>
        /// <returns>A new empty {@code list} instance.</returns>
        IonList NewEmptyList();

        /// <summary>
        /// Constructs a new {@code null.null} instance.
        /// </summary>
        /// <returns>A new {@code null.null} instance.</returns>
        IonNull NewNull();

        /// <summary>
        /// Constructs a new {@code null.sexp} instance.
        /// </summary>
        /// <returns>A new {@code null.sexp} instance.</returns>
        IonSexp NewNullSexp();

        /// <summary>
        /// Constructs a new empty (not null) {@code sexp} instance.
        /// </summary>
        /// <returns>A new empty {@code sexp} instance.</returns>
        IonSexp NewSexp();

        /// <summary>
        /// Constructs a new Ion string with the given value.
        /// </summary>
        /// <param name="value">The value of the text for the new string.</param>
        /// <returns>A new {@code string} instance.</returns>
        IonString NewString(string value);

        /// <summary>
        /// Constructs a new {@code null.struct} instance.
        /// </summary>
        /// <returns>A new {@code null.struct} instance.</returns>
        IonStruct NewNullStruct();

        /// <summary>
        /// Constructs a new empty (not null) {@code struct} instance.
        /// </summary>
        /// <returns>A new empty {@code struct} instance.</returns>
        IonStruct NewEmptyStruct();

        /// <summary>
        /// Constructs a new {@code null.symbol} instance.
        /// </summary>
        /// <returns>A new {@code null.symbol} instance.</returns>
        IonSymbol NewNullSymbol();

        /// <summary>
        /// Constructs a new Ion symbol with the given symbol token.
        /// </summary>
        /// <param name="symbolToken">The value the text and/or SID of the symbol.</param>
        /// <returns>A new {@code symbol} instance.</returns>
        IonSymbol NewSymbol(SymbolToken symbolToken);

        /// <summary>
        /// Constructs a new Ion symbol with the given values.
        /// </summary>
        /// <param name="text">The text value for the new symbol.</param>
        /// <param name="sid">The SID value for the new symbol.</param>
        /// <returns>A new {@code symbol} instance.</returns>
        IonSymbol NewSymbol(string text, int sid);

        /// <summary>
        /// Constructs a new {@code null.timestamp} instance.
        /// </summary>
        /// <returns>A new {@code null.timestamp} instance.</returns>
        IonTimestamp NewNullTimestamp();

        /// <summary>
        /// Constructs a new {@code timestamp} instance with the given value.
        /// </summary>
        /// <param name="val">The value for the new timestamp.</param>
        /// <returns>A new {@code timestamp} instance.</returns>
        IonTimestamp NewTimestamp(Timestamp val);
    }
}
