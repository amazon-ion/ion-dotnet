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
        IonBlob NewNullBlob();
        IonBlob NewBlob(ReadOnlySpan<byte> bytes);

        IonBool NewNullBool();
        IonBool NewBool(bool value);

        IonClob NullClob();
        IonClob NewClob(ReadOnlySpan<byte> bytes);

        IonDecimal NewNullDecimal();
        IonDecimal NewDecimal(double doubleValue);
        IonDecimal NewDecimal(decimal value);
        IonDecimal NewDecimal(BigDecimal bigDecimal);

        IonFloat NewNullFloat();
        IonFloat NewFloat(double value);

        IonInt NewNullInt();
        IonInt NewInt(long value);
        IonInt NewInt(BigInteger value);

        IonList NewNullList();
        IonList NewEmptyList();

        IonNull NewNull();

        IonSexp NewNullSexp();
        IonSexp NewSexp();

        IonString NewString(string value);

        IonStruct NewNullStruct();
        IonStruct NewEmptyStruct();

        IonSymbol NewNullSymbol();
        IonSymbol NewSymbol(SymbolToken symbolToken);
        IonSymbol NewSymbol(string text, int sid);

        IonTimestamp NewNullTimestamp();
        IonTimestamp NewTimestamp(Timestamp val);
    }
}
