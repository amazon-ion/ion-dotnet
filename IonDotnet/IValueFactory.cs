using System;
using System.Collections.Generic;

namespace IonDotnet
{
    public interface IValueFactory
    {
        T Clone<T>(T value) where T : IIonValue;

        #region Blob

        IIonBlob NewNullBlob();

        IIonBlob NewBlob(byte[] bytes);

        IIonBlob NewBlob(ArraySegment<byte> bytes);

        IIonBlob NewBlob(Span<byte> bytes);

        #endregion

        #region Bool

        IIonBool NewNullBool();

        IIonBool NewBool(bool value);

        #endregion

        #region Clob

        IIonClob NewNullClob();

        IIonClob NewClob(byte[] data);

        IIonClob NewClob(ArraySegment<byte> data);

        IIonClob NewClob(Span<byte> data);

        #endregion

        #region Decimal

        IIonDecimal NewNullDecimal();

        IIonDecimal NewDecimal(decimal value);

        IIonDecimal NewDecimal(long value);

        IIonDecimal NewDecimal(double value);

        #endregion

        #region Float

        IIonFloat NewNullFloat();

        IIonFloat NewFloat(double value);

        IIonFloat NewFloat(long value);

        #endregion

        #region Integer

        IIonInt NewNullInt();

        IIonInt NewInt(int value);

        IIonInt NewInt(long value);

        #endregion

        #region List

        IIonList NewNullList();

        IIonList NewEmptyList();

        IIonList NewList(IIonSequence children);

        IIonList NewList(params IIonValue[] children);

        IIonList NewList(IEnumerable<int> values);

        IIonList NewList(IEnumerable<long> values);

        #endregion

        #region Null

        IIonNull NewNull();

        IIonValue NewNull(IonType type);

        #endregion

        #region Sexp

        #endregion

        #region String

        IIonValue NewNullString();

        IIonValue NewString(string value);

        #endregion

        #region Struct

        IIonStruct NewNullStruct();

        IIonStruct NewStruct();

        #endregion

        #region Symbol

        IIonSymbol NewNullSymbol();

        IIonSymbol NewSymbol(string text);

        IIonSymbol NewSymbol(SymbolToken token);

        #endregion

        #region TimeStamp

        IIonTimestamp NewNullTimestamp();

        IIonTimestamp NewTimestamp(DateTimeOffset dateTimeOffset);

        #endregion
    }
}
