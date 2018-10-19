using System;
using System.Collections.Generic;
using System.Numerics;
using IonDotnet.Conversions;

namespace IonDotnet
{
    /// <summary>
    /// Provides stream-based access to Ion data independent of its underlying representation (text, binary, or {@link IonValue} tree).
    /// </summary>
    public interface IIonReader
    {
        /// <summary>
        /// Positions this reader on the next sibling after the current value
        /// </summary>
        /// <returns>Type of the current value</returns>
        IonType MoveNext();

        /// <summary>
        /// Positions the reader just before the contents of the current value, which must be a container (list, sexp, or struct).
        /// </summary>
        /// <exception cref="InvalidOperationException">When the current value is not an <see cref="IonDotnet.Tree.IonContainer"/></exception>
        /// <remarks>
        /// There's no current value immediately after stepping in, so the next thing you'll want to do is call <see cref="MoveNext"/>
        /// to move onto the first child value (or learn that there's not one).
        /// </remarks>
        void StepIn();

        /// <summary>
        /// Positions the iterator after the current parent's value, moving up one level in the data hierarchy.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>
        /// There's no current value immediately after stepping out, so the next thing you'll want to do is call <see cref="MoveNext"/>
        /// to move onto the following value
        /// </remarks>
        void StepOut();

        /// <summary>
        /// Depth into the Ion value that this reader has traversed. At top level the depth is 0, and it increases
        /// by 1 by each call to <see cref="StepIn"/>
        /// </summary>
        int CurrentDepth { get; }

        /// <summary>
        /// Returns the symbol table that is applicable to the current value.
        /// </summary>
        /// <returns>Symbol table that is applicable to the current value.</returns>
        /// <remarks>This may be either a system or local symbol table.</remarks>
        ISymbolTable GetSymbolTable();

        /// <summary>
        /// Returns the type of the current value, or null if there is no current value
        /// </summary>
        /// <returns></returns>
        IonType CurrentType { get; }

        /// <returns>
        /// Smallest possible size representing the smallest-possible, or <see cref="IntegerSize.Unknown"/>
        /// if there is no current value or the current value is a 'null'
        /// </returns>
        IntegerSize GetIntegerSize();

        /// <value>
        /// Return the field name of the current value. Or null if there is no valid current value
        /// </value>
        string CurrentFieldName { get; }

        /// <summary>
        /// Get the field name of the current value as a <see cref="SymbolToken"/>.
        /// </summary>
        SymbolToken GetFieldNameSymbol();

        /// <value>True if the current value is a null</value>
        bool CurrentIsNull { get; }

        /// <value>True if reading a struct</value>
        bool IsInStruct { get; }

        /// <returns>Current boolean value</returns>
        bool BoolValue();

        /// <returns>Current int value</returns>
        int IntValue();

        /// <returns>Current long value</returns>
        long LongValue();

        /// <returns>Current BigInteger value</returns>
        BigInteger BigIntegerValue();

        /// <returns>Current double value</returns>
        double DoubleValue();

        /// <returns>Current decimal value</returns>
        BigDecimal DecimalValue();

        /// <returns>Current timestamp value</returns>
        Timestamp TimestampValue();

        /// <returns>Current string value</returns>
        string StringValue();

        /// <returns>Current symbol value</returns>
        SymbolToken SymbolValue();

        /// <summary>
        /// Get the size of the current lob value
        /// </summary>
        /// <returns>Size of the current lob value</returns>
        int GetLobByteSize();

        /// <summary>
        /// Copy the current blob value to a new byte array
        /// </summary>
        /// <returns>Reference to new byte array</returns>
        byte[] NewByteArray();

        /// <summary>
        /// Copy the current blob value to a buffer
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <returns>Number of bytes copied</returns>
        int GetBytes(Span<byte> buffer);

        IEnumerable<SymbolToken> GetTypeAnnotations();

        bool TryConvertTo(Type targetType, IScalarConverter scalarConverter, out object result);
    }
}
