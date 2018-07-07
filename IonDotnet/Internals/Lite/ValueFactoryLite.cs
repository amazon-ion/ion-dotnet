using System;
using System.Collections.Generic;

namespace IonDotnet.Internals.Lite
{
    internal abstract class ValueFactoryLite : IPrivateValueFactory
    {
        public abstract T Clone<T>(T value) where T : IIonValue;
        public abstract IIonBlob NewNullBlob();
        public abstract IIonBlob NewBlob(byte[] bytes);
        public abstract IIonBlob NewBlob(ArraySegment<byte> bytes);
        public abstract IIonBlob NewBlob(Span<byte> bytes);
        public abstract IIonBool NewNullBool();
        public abstract IIonBool NewBool(bool value);
        public abstract IIonClob NewNullClob();
        public abstract IIonClob NewClob(byte[] data);
        public abstract IIonClob NewClob(ArraySegment<byte> data);
        public abstract IIonClob NewClob(Span<byte> data);
        public abstract IIonDecimal NewNullDecimal();
        public abstract IIonDecimal NewDecimal(decimal value);
        public abstract IIonDecimal NewDecimal(long value);
        public abstract IIonDecimal NewDecimal(double value);
        public abstract IIonFloat NewNullFloat();
        public abstract IIonFloat NewFloat(double value);
        public abstract IIonFloat NewFloat(long value);
        public abstract IIonInt NewNullInt();
        public abstract IIonInt NewInt(int value);
        public abstract IIonInt NewInt(long value);
        public abstract IIonList NewNullList();
        public abstract IIonList NewEmptyList();
        public abstract IIonList NewList(IIonSequence children);
        public abstract IIonList NewList(params IIonValue[] children);
        public abstract IIonList NewList(IEnumerable<int> values);
        public abstract IIonList NewList(IEnumerable<long> values);
        public abstract IIonNull NewNull();
        public abstract IIonValue NewNull(IonType type);
        public abstract IIonValue NewNullString();
        public abstract IIonValue NewString(string value);
        public abstract IIonValue NewString(Span<char> value);
        public abstract IIonStruct NewNullStruct();
        public abstract IIonStruct NewStruct();
        public abstract IIonSymbol NewNullSymbol();
        public abstract IIonSymbol NewSymbol(string text);
        public abstract IIonSymbol NewSymbol(SymbolToken token);
        public abstract IIonTimestamp NewNullTimestamp();
        public abstract IIonTimestamp NewTimestamp(DateTimeOffset dateTimeOffset);
    }
}
