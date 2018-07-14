using System;
using System.Numerics;

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
        IonType Next();

        /// <summary>
        /// Positions the reader just before the contents of the current value, which must be a container (list, sexp, or struct).
        /// </summary>
        /// <exception cref="InvalidOperationException">When the current value is not an <see cref="IIonContainer"/></exception>
        /// <remarks>
        /// There's no current value immediately after stepping in, so the next thing you'll want to do is call <see cref="Next"/>
        /// to move onto the first child value (or learn that there's not one).
        /// </remarks>
        void StepIn();

        /// <summary>
        /// Positions the iterator after the current parent's value, moving up one level in the data hierarchy.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>
        /// There's no current value immediately after stepping out, so the next thing you'll want to do is call <see cref="Next"/>
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
        IonType GetCurrentType();

        /// <returns>
        /// Smallest possible size representing the smallest-possible, or <see cref="IntegerSize.Unknown"/>
        /// if there is no current value or the current value is a 'null'
        /// </returns>
        IntegerSize GetIntegerSize();

        /// <returns>
        /// Return the field name of the current value. Or null if there is no valid current value
        /// </returns>
        string GetFieldName();

        SymbolToken GetFieldNameSymbol();

        bool CurrentIsNull { get; }
        bool IsInStruct { get; }

        bool BoolValue();

        int IntValue();

        long LongValue();

        BigInteger BigIntegerValue();

        double DoubleValue();

        decimal DecimalValue();

        DateTime DateTimeValue();

        string StringValue();

        SymbolToken SymbolValue();

        int GetLobByteSize();

        byte[] NewByteArray();

        int GetBytes(ArraySegment<byte> buffer);
    }
}
