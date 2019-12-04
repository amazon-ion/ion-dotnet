using System;
using System.Numerics;

namespace IonDotnet.Tree.Impl
{
    public class ValueFactory : IValueFactory
    {
        public ValueFactory()
        {
        }

        public IonBlob NewNullBlob()
        {
            return IonBlob.NewNull();
        }

        public IonBlob NewBlob(ReadOnlySpan<byte> bytes)
        {
            return new IonBlob(bytes);
        }

        public IonBool NewNullBool()
        {
            return IonBool.NewNull();
        }

        public IonBool NewBool(bool value)
        {
            return new IonBool(value);
        }

        public IonClob NullClob()
        {
            return IonClob.NewNull();
        }

        public IonClob NewClob(ReadOnlySpan<byte> bytes)
        {
            return new IonClob(bytes);
        }

        public IonDecimal NewNullDecimal()
        {
            return IonDecimal.NewNull();
        }

        public IonDecimal NewDecimal(double doubleValue)
        {
            return new IonDecimal(doubleValue);
        }

        public IonDecimal NewDecimal(decimal value)
        {
            return new IonDecimal(value);
        }

        public IonDecimal NewDecimal(BigDecimal bigDecimal)
        {
            return new IonDecimal(bigDecimal);
        }

        public IonFloat NewNullFloat()
        {
            return IonFloat.NewNull();
        }

        public IonFloat NewFloat(double value)
        {
            return new IonFloat(value);
        }

        public IonInt NewNullInt()
        {
            return IonInt.NewNull();
        }

        public IonInt NewInt(long value)
        {
            return new IonInt(value);
        }

        public IonInt NewInt(BigInteger value)
        {
            return new IonInt(value);
        }

        public IonList NewNullList()
        {
            return IonList.NewNull();
        }

        public IonList NewEmptyList()
        {
            return new IonList();
        }

        public IonNull NewNull()
        {
            return new IonNull();
        }

        public IonSexp NewNullSexp()
        {
            return IonSexp.NewNull();
        }

        public IonSexp NewSexp()
        {
            return new IonSexp();
        }

        public IonString NewString(string value)
        {
            return new IonString(value);
        }

        public IonStruct NewNullStruct()
        {
            return IonStruct.NewNull();
        }

        public IonStruct NewEmptyStruct()
        {
            return new IonStruct();
        }

        public IonSymbol NewNullSymbol()
        {
            return IonSymbol.NewNull();
        }

        public IonSymbol NewSymbol(SymbolToken symbolToken)
        {
            return new IonSymbol(symbolToken);
        }

        public IonSymbol NewSymbol(string text, int sid)
        {
            return new IonSymbol(text, sid);
        }

        public IonTimestamp NewNullTimestamp()
        {
            return IonTimestamp.NewNull();
        }

        public IonTimestamp NewTimestamp(Timestamp val)
        {
            return new IonTimestamp(val);
        }
    }
}
