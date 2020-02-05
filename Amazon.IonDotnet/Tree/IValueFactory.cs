/*
 * Copyright 2020 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

using System;
using System.Numerics;

namespace Amazon.IonDotnet.Tree
{
    /// <summary>
    /// The factory for all <c>IonValue</c>s.
    /// </summary>
    public interface IValueFactory
    {
        /// <summary>
        /// Constructs a new <c>null.blob</c> instance.
        /// </summary>
        /// <returns>A new <c>null.blob</c> instance, which implements the <see cref="IIonBlob"/> interface methods.</returns>
        IIonValue NewNullBlob();

        /// <summary>
        /// Constructs a new Ion <c>blob</c> instance, copying bytes from an array.
        /// </summary>
        /// <param name="bytes">The data for the new blob.</param>
        /// <returns>A new Ion <c>blob</c> instance, which implements the <see cref="IIonBlob"/> interface methods.</returns>
        IIonValue NewBlob(ReadOnlySpan<byte> bytes);

        /// <summary>
        /// Constructs a new <c>null.bool</c> instance.
        /// </summary>
        /// <returns>A new <c>null.bool</c> instance, which implements the <see cref="IIonBool"/> interface methods.</returns>
        IIonValue NewNullBool();

        /// <summary>
        /// Constructs a new <c>bool</c> instance with the given value.
        /// </summary>
        /// <param name="value">The new <c>bool</c>'s value.</param>
        /// <returns>A <c>bool</c> initialized with the provided value, which implements the <see cref="IIonBool"/> interface methods.</returns>
        IIonValue NewBool(bool value);

        /// <summary>
        /// Constructs a new <c>null.clob</c> instance.
        /// </summary>
        /// <returns>A new <c>null.clob</c> instance, which implements the <see cref="IIonClob"/> interface methods.</returns>
        IIonValue NewNullClob();

        /// <summary>
        /// Constructs a new Ion <c>clob</c> instance, copying bytes from an array.
        /// </summary>
        /// <param name="bytes">The data for the new clob.</param>
        /// <returns>A new Ion <c>clob</c> instance, which implements the <see cref="IIonClob"/> interface methods.</returns>
        IIonValue NewClob(ReadOnlySpan<byte> bytes);

        /// <summary>
        /// Constructs a new <c>null.decimal</c> instance.
        /// </summary>
        /// <returns>A new <c>null.decimal</c> instance, which implements the <see cref="IIonDecimal"/> interface methods.</returns>
        IIonValue NewNullDecimal();

        /// <summary>
        /// Constructs a new Ion <c>decimal</c> instance from a C# <c>double</c>.
        /// </summary>
        /// <param name="doubleValue">The value for the new decimal.</param>
        /// <returns>A new Ion <c>decimal</c> instance, which implements the <see cref="IIonDecimal"/> interface methods.</returns>
        IIonValue NewDecimal(double doubleValue);

        /// <summary>
        /// Constructs a new Ion <c>decimal</c> instance from a C# <c>decimal</c>.
        /// </summary>
        /// <param name="value">The value for the new decimal.</param>
        /// <returns>A new Ion <c>decimal</c> instance, which implements the <see cref="IIonDecimal"/> interface methods.</returns>
        IIonValue NewDecimal(decimal value);

        /// <summary>
        /// Constructs a new Ion <c>decimal</c> instance from a C# <c>BigDecimal</c>.
        /// </summary>
        /// <param name="bigDecimal">The value for the new decimal.</param>
        /// <returns>A new Ion <c>decimal</c> instance, which implements the <see cref="IIonDecimal"/> interface methods.</returns>
        IIonValue NewDecimal(BigDecimal bigDecimal);

        /// <summary>
        /// Constructs a new <c>null.float</c> instance.
        /// </summary>
        /// <returns>A new <c>null.float</c> instance, which implements the <see cref="IIonFloat"/> interface methods.</returns>
        IIonValue NewNullFloat();

        /// <summary>
        /// Constructs a new Ion <c>float</c> instance from a C# <c>double</c>.
        /// </summary>
        /// <param name="value">The value for the new float.</param>
        /// <returns>A new Ion <c>float</c> instance, which implements the <see cref="IIonFloat"/> interface methods.</returns>
        IIonValue NewFloat(double value);

        /// <summary>
        /// Constructs a new <c>null.int</c> instance.
        /// </summary>
        /// <returns>A new <c>null.int</c> instance, which implements the <see cref="IIonInt"/> interface methods.</returns>
        IIonValue NewNullInt();

        /// <summary>
        /// Constructs a new <c>int</c> instance with the given value.
        /// </summary>
        /// <param name="value">The value for the new int.</param>
        /// <returns>A new <c>int</c> instance, which implements the <see cref="IIonInt"/> interface methods.</returns>
        IIonValue NewInt(long value);

        /// <summary>
        /// Constructs a new <c>int</c> instance with the given value.
        /// </summary>
        /// <param name="value">The value for the new int.</param>
        /// <returns>A new <c>int</c> instance, which implements the <see cref="IIonInt"/> interface methods.</returns>
        IIonValue NewInt(BigInteger value);

        /// <summary>
        /// Constructs a new <c>null.list</c> instance.
        /// </summary>
        /// <returns>A new <c>null.list</c> instance, which implements the <see cref="IIonList"/> interface methods.</returns>
        IIonValue NewNullList();

        /// <summary>
        /// Constructs a new empty (not null) <c>list</c> instance.
        /// </summary>
        /// <returns>A new empty <c>list</c> instance, which implements the <see cref="IIonList"/> interface methods.</returns>
        IIonValue NewEmptyList();

        /// <summary>
        /// Constructs a new <c>null.null</c> instance.
        /// </summary>
        /// <returns>A new <c>null.null</c> instance, which implements the <see cref="IIonNull"/> interface methods.</returns>
        IIonValue NewNull();

        /// <summary>
        /// Constructs a new {@code null.sexp} instance.
        /// </summary>
        /// <returns>A new {@code null.sexp} instance, which implements the <see cref="IIonSexp"/> interface methods.</returns>
        IIonValue NewNullSexp();

        /// <summary>
        /// Constructs a new empty (not null) <c>sexp</c> instance.
        /// </summary>
        /// <returns>A new empty <c>sexp</c> instance, which implements the <see cref="IIonSexp"/> interface methods.</returns>
        IIonValue NewEmptySexp();

        /// <summary>
        /// Constructs a new <c>null.string</c> instance.
        /// </summary>
        /// <returns>A new <c>null.string</c> instance, which implements the <see cref="IIonString"/> interface methods.</returns>
        IIonValue NewNullString();

        /// <summary>
        /// Constructs a new Ion string with the given value.
        /// </summary>
        /// <param name="value">The value of the text for the new string.</param>
        /// <returns>A new <c>string</c> instance, which implements the <see cref="IIonString"/> interface methods.</returns>
        IIonValue NewString(string value);

        /// <summary>
        /// Constructs a new <c>null.struct</c> instance.
        /// </summary>
        /// <returns>A new <c>null.struct</c> instance, which implements the <see cref="IIonStruct"/> interface methods.</returns>
        IIonValue NewNullStruct();

        /// <summary>
        /// Constructs a new empty (not null) <c>struct</c> instance.
        /// </summary>
        /// <returns>A new empty <c>struct</c> instance, which implements the <see cref="IIonStruct"/> interface methods.</returns>
        IIonValue NewEmptyStruct();

        /// <summary>
        /// Constructs a new <c>null.symbol</c> instance.
        /// </summary>
        /// <returns>A new <c>null.symbol</c> instance, which implements the <see cref="IIonSymbol"/> interface methods.</returns>
        IIonValue NewNullSymbol();

        /// <summary>
        /// Constructs a new Ion symbol with the given symbol token.
        /// </summary>
        /// <param name="symbolToken">The value the text and/or SID of the symbol.</param>
        /// <returns>A new <c>symbol</c> instance, which implements the <see cref="IIonSymbol"/> interface methods.</returns>
        IIonValue NewSymbol(SymbolToken symbolToken);

        /// <summary>
        /// Constructs a new Ion symbol with the given values.
        /// </summary>
        /// <param name="text">The text value for the new symbol.</param>
        /// <returns>A new <c>symbol</c> instance, which implements the <see cref="IIonSymbol"/> interface methods.</returns>
        IIonValue NewSymbol(string text);

        /// <summary>
        /// Constructs a new <c>null.timestamp</c> instance.
        /// </summary>
        /// <returns>A new <c>null.timestamp</c> instance, which implements the <see cref="IIonTimestamp"/> interface methods.</returns>
        IIonValue NewNullTimestamp();

        /// <summary>
        /// Constructs a new <c>timestamp</c> instance with the given value.
        /// </summary>
        /// <param name="val">The value for the new timestamp.</param>
        /// <returns>A new <c>timestamp</c> instance, which implements the <see cref="IIonTimestamp"/> interface methods.</returns>
        IIonValue NewTimestamp(Timestamp val);
    }
}
