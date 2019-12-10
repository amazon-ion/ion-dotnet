using System;
using System.Numerics;

namespace IonDotnet.Tree.Impl
{
    public class ValueFactory : IValueFactory
    {
        public ValueFactory()
        {
        }

        public IIonBlob NewNullBlob()
        {
            return IonBlob.NewNull();
        }

        public IIonBlob NewBlob(ReadOnlySpan<byte> bytes)
        {
            return new IonBlob(bytes);
        }

        public IIonBool NewNullBool()
        {
            return IonBool.NewNull();
        }

        public IIonBool NewBool(bool value)
        {
            return new IonBool(value);
        }

        public IIonClob NullClob()
        {
            return IonClob.NewNull();
        }

        public IIonClob NewClob(ReadOnlySpan<byte> bytes)
        {
            return new IonClob(bytes);
        }

        public IIonDecimal NewNullDecimal()
        {
            return IonDecimal.NewNull();
        }

        public IIonDecimal NewDecimal(double doubleValue)
        {
            return new IonDecimal(doubleValue);
        }

        public IIonDecimal NewDecimal(decimal value)
        {
            return new IonDecimal(value);
        }

        public IIonDecimal NewDecimal(BigDecimal bigDecimal)
        {
            return new IonDecimal(bigDecimal);
        }

        public IIonFloat NewNullFloat()
        {
            return IonFloat.NewNull();
        }

        public IIonFloat NewFloat(double value)
        {
            return new IonFloat(value);
        }

        public IIonInt NewNullInt()
        {
            return IonInt.NewNull();
        }

        public IIonInt NewInt(long value)
        {
            return new IonInt(value);
        }

        public IIonInt NewInt(BigInteger value)
        {
            return new IonInt(value);
        }

        public IIonList NewNullList()
        {
            return IonList.NewNull();
        }

        public IIonList NewEmptyList()
        {
            return new IonList();
        }

        public IIonNull NewNull()
        {
            return new IonNull();
        }

        public IIonSexp NewNullSexp()
        {
            return IonSexp.NewNull();
        }

        public IIonSexp NewSexp()
        {
            return new IonSexp();
        }

        public IIonString NewString(string value)
        {
            return new IonString(value);
        }

        public IIonStruct NewNullStruct()
        {
            return IonStruct.NewNull();
        }

        public IIonStruct NewEmptyStruct()
        {
            return new IonStruct();
        }

        public IIonSymbol NewNullSymbol()
        {
            return IonSymbol.NewNull();
        }

        public IIonSymbol NewSymbol(SymbolToken symbolToken)
        {
            return new IonSymbol(symbolToken);
        }

        public IIonSymbol NewSymbol(string text, int sid)
        {
            return new IonSymbol(text, sid);
        }

        public IIonTimestamp NewNullTimestamp()
        {
            return IonTimestamp.NewNull();
        }

        public IIonTimestamp NewTimestamp(Timestamp val)
        {
            return new IonTimestamp(val);
        }
    }
}
