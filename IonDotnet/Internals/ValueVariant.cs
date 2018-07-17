using System;
using System.Numerics;

namespace IonDotnet.Internals
{
    public struct ValueVariant
    {
        public ScalarType TypeSet { get; private set; }
        public ScalarType AuthoritativeType { get; private set; }

        public bool BoolValue { get; private set; }
        public int IntValue { get; private set; }
        public long LongValue { get; private set; }
        public double DoubleValue { get; private set; }
        public string StringValue { get; private set; }
        public BigInteger BigIntegerValue { get; private set; }
        public decimal DecimalValue { get; private set; }
        public DateTime DatetimeValue { get; private set; }
        //TODO datetime

        public bool IsEmpty => AuthoritativeType == ScalarType.Nothing;

        public void Clear()
        {
            AuthoritativeType = ScalarType.Nothing;
            TypeSet = ScalarType.Nothing;
            StringValue = null;
        }

        internal void SetNull(IonType ionType)
        {
            switch (ionType)
            {
                default:
                    throw new ArgumentOutOfRangeException(nameof(ionType));
                case IonType.Int:
                    AuthoritativeType = ScalarType.Int;
                    break;
                case IonType.Decimal:
                    AuthoritativeType = ScalarType.Decimal;
                    break;
                case IonType.Null:
                    AuthoritativeType = ScalarType.Null;
                    break;
                case IonType.Bool:
                    AuthoritativeType = ScalarType.Bool;
                    break;
                case IonType.String:
                    AuthoritativeType = ScalarType.String;
                    break;
                case IonType.Timestamp:
                    AuthoritativeType = ScalarType.DateTime;
                    break;
                case IonType.Symbol:
                    AuthoritativeType = ScalarType.Int;
                    break;
                case IonType.Float:
                    AuthoritativeType = ScalarType.Double;
                    break;
            }
            TypeSet = ScalarType.Null;
        }

        internal void AddValue<T>(T value)
        {
            switch (value)
            {
                default:
                    throw new ArgumentOutOfRangeException($"Cannot set type {value.GetType()}");
                case bool boolValue:
                    BoolValue = boolValue;
                    TypeSet |= ScalarType.Bool;
                    break;
                case int intValue:
                    IntValue = intValue;
                    TypeSet |= ScalarType.Int;
                    break;
                case long longVal:
                    LongValue = longVal;
                    TypeSet |= ScalarType.Long;
                    break;
                case BigInteger bigInt:
                    BigIntegerValue = bigInt;
                    TypeSet |= ScalarType.BigInteger;
                    break;
                case decimal decimalValue:
                    DecimalValue = decimalValue;
                    TypeSet |= ScalarType.Decimal;
                    break;
                case double doubleVal:
                    DoubleValue = doubleVal;
                    TypeSet |= ScalarType.Double;
                    break;
                case string stringVal:
                    StringValue = stringVal;
                    TypeSet |= ScalarType.String;
                    break;
                case DateTime dateTime:
                    DatetimeValue = dateTime;
                    TypeSet |= ScalarType.DateTime;
                    break;
            }
        }

        internal void SetValue<T>(T value)
        {
            switch (value)
            {
                default:
                    throw new ArgumentOutOfRangeException($"Cannot set type {value.GetType()}");
                case bool boolValue:
                    BoolValue = boolValue;
                    TypeSet = ScalarType.Bool;
                    AuthoritativeType = ScalarType.Bool;
                    break;
                case int intValue:
                    IntValue = intValue;
                    TypeSet = ScalarType.Int;
                    AuthoritativeType = ScalarType.Int;
                    break;
                case long longVal:
                    LongValue = longVal;
                    TypeSet = ScalarType.Long;
                    AuthoritativeType = ScalarType.Long;
                    break;
                case BigInteger bigInt:
                    BigIntegerValue = bigInt;
                    TypeSet = ScalarType.BigInteger;
                    AuthoritativeType = ScalarType.BigInteger;
                    break;
                case decimal decimalValue:
                    DecimalValue = decimalValue;
                    TypeSet = ScalarType.Decimal;
                    AuthoritativeType = ScalarType.Decimal;
                    break;
                case double doubleVal:
                    DoubleValue = doubleVal;
                    TypeSet = ScalarType.Double;
                    AuthoritativeType = ScalarType.Double;
                    break;
                case string stringVal:
                    StringValue = stringVal;
                    TypeSet = ScalarType.String;
                    AuthoritativeType = ScalarType.String;
                    break;
                case DateTime dateTime:
                    DatetimeValue = dateTime;
                    TypeSet = ScalarType.DateTime;
                    AuthoritativeType = ScalarType.DateTime;
                    break;
            }
        }

        public IntegerSize IntegerSize
        {
            get
            {
                switch (AuthoritativeType)
                {
                    default:
                        return IntegerSize.Unknown;
                    case ScalarType.Long:
                        return IntegerSize.Long;
                    case ScalarType.Int:
                        return IntegerSize.Int;
                    case ScalarType.BigInteger:
                        return IntegerSize.BigInteger;
                }
            }
        }
    }
}
