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

namespace Amazon.IonDotnet
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;

    /// <summary>
    /// Provides stream-based access to Ion data independent of its underlying representation (text, binary, or {@link IonValue} tree).
    /// </summary>
    public interface IIonReader : IDisposable
    {
        /// <summary>
        /// Gets the depth into the Ion value that this reader has traversed. At top level the depth is 0, and it increases
        /// by 1 by each call to <see cref="StepIn"/>.
        /// </summary>
        int CurrentDepth { get; }

        /// <summary>
        /// Gets the type of the current value, or null if there is no current value.
        /// </summary>
        /// <returns>Type of current value.</returns>
        IonType CurrentType { get; }

        /// <summary>
        /// Gets the fueld name of the current value.
        /// </summary>
        /// <value>
        /// Return the field name of the current value. Or null if there is no valid current value.
        /// </value>
        string CurrentFieldName { get; }

        /// <summary>Gets a value indicating whether the current value is a null.</summary>
        /// <value>True if the current value is a null.</value>
        bool CurrentIsNull { get; }

        /// <summary>Gets a value indicating whether the current value is in a struct.</summary>
        /// <value>True if reading a struct.</value>
        bool IsInStruct { get; }

        /// <summary>
        /// Get the field name of the current value as a <see cref="SymbolToken"/>.
        /// </summary>
        /// <returns>
        /// The field name of the current value as a <see cref="SymbolToken"/>.
        /// </returns>
        SymbolToken GetFieldNameSymbol();

        /// <summary>
        /// Positions this reader on the next sibling after the current value.
        /// </summary>
        /// <returns>Type of the current value.</returns>
        IonType MoveNext();

        /// <summary>
        /// Positions the reader just before the contents of the current value, which must be a container (list, sexp, or struct).
        /// </summary>
        /// <exception cref="InvalidOperationException">When the current value is not an <see cref="IonDotnet.Tree.IonContainer"/>.</exception>
        /// <remarks>
        /// There's no current value immediately after stepping in, so the next thing you'll want to do is call <see cref="MoveNext"/>
        /// to move onto the first child value (or learn that there's not one).
        /// </remarks>
        void StepIn();

        /// <summary>
        /// Positions the iterator after the current parent's value, moving up one level in the data hierarchy.
        /// </summary>
        /// <exception cref="InvalidOperationException">When the current value cannot be stepped into.</exception>
        /// <remarks>
        /// There's no current value immediately after stepping out, so the next thing you'll want to do is call <see cref="MoveNext"/>
        /// to move onto the following value.
        /// </remarks>
        void StepOut();

        /// <summary>
        /// Returns the symbol table that is applicable to the current value.
        /// </summary>
        /// <returns>Symbol table that is applicable to the current value.</returns>
        /// <remarks>This may be either a system or local symbol table.</remarks>
        ISymbolTable GetSymbolTable();

        /// <summary>
        /// Returns the smallest-possible C# type of an IonInt value.
        /// </summary>
        /// <returns>
        /// Smallest-possible C# type of an IonInt value, or <see cref="IntegerSize.Unknown"/>
        /// if there is no current value or the current value is a 'null'.
        /// </returns>
        IntegerSize GetIntegerSize();

        /// <summary>Returns the current value as a Boolean.</summary>
        /// <returns>Current boolean value.</returns>
        bool BoolValue();

        /// <summary>Returns the current value as a int.</summary>
        /// <returns>Current int value.</returns>
        int IntValue();

        /// <summary>Returns the current value as a long.</summary>
        /// <returns>Current long value.</returns>
        long LongValue();

        /// <summary>Returns the current value as a BigInteger.</summary>
        /// <returns>Current BigInteger value.</returns>
        BigInteger BigIntegerValue();

        /// <summary>Returns the current value as a double.</summary>
        /// <returns>Current double value.</returns>
        double DoubleValue();

        /// <summary>Returns the current value as a decimal.</summary>
        /// <returns>Current decimal value.</returns>
        BigDecimal DecimalValue();

        /// <summary>Returns the current value as a Timestamp.</summary>
        /// <returns>Current timestamp value.</returns>
        Timestamp TimestampValue();

        /// <summary>Returns the current value as a string.</summary>
        /// <returns>Current string value.</returns>
        string StringValue();

        /// <summary>Returns th current value as a SymbolToken.</summary>
        /// <returns>Current symbol value.</returns>
        SymbolToken SymbolValue();

        /// <summary>
        /// Get the size of the current lob value.
        /// </summary>
        /// <returns>Size of the current lob value.</returns>
        int GetLobByteSize();

        /// <summary>
        /// Copy the current blob value to a new byte array.
        /// </summary>
        /// <returns>Reference to new byte array.</returns>
        byte[] NewByteArray();

        /// <summary>
        /// Copy the current blob value to a buffer.
        /// </summary>
        /// <param name="buffer">Buffer.</param>
        /// <returns>Number of bytes copied.</returns>
        int GetBytes(Span<byte> buffer);

        /// <summary>
        /// Get the annotations of the current value as an array of strings.
        /// </summary>
        /// <returns>
        /// The (ordered) annotations on the current value, or an empty array
        /// if there are none.
        /// </returns>
        /// <exception cref="UnknownSymbolException">If any annotation has unknown text.</exception>
        string[] GetTypeAnnotations();

        /// <summary>
        /// Gets the current value's annotations as symbol tokens (text + ID).
        /// </summary>
        /// <returns>
        /// The (ordered) annotations on the current value, or an empty array
        /// if there are none.
        /// </returns>
        IEnumerable<SymbolToken> GetTypeAnnotationSymbols();

        /// <summary>
        /// Determines if the current value contains such annotation.
        /// </summary>
        /// <returns>
        /// True if the current value contains such annotation.
        /// Otherwise, False.
        /// </returns>
        /// <param name="annotation">Annotation text.</param>
        /// <exception cref="ArgumentNullException">When annotation is null.</exception>
        /// <exception cref="UnknownSymbolException">If a match is not found and any annotation has unknown text.</exception>
        bool HasAnnotation(string annotation);
    }
}
