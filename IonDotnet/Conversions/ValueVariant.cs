using System;
using System.Numerics;

namespace IonDotnet.Conversions
{
    /// <summary>
    /// Structure that holds the loaded scalar value from IonReader
    /// </summary>
    public struct ValueVariant
    {
        private int _intValue;
        private long _longValue;
        private double _doubleValue;
        private string _stringValue;
        private BigInteger _bigIntegerValue;
        private BigDecimal _decimalValue;
        private Timestamp _timestampValue;
        private bool _boolValue;

        public bool BoolValue
        {
            get => _boolValue;
            internal set
            {
                _boolValue = value;
                AuthoritativeType = ScalarType.Bool;
                TypeSet = ScalarType.Bool;
            }
        }

        public int IntValue
        {
            get => _intValue;
            internal set
            {
                _intValue = value;
                _longValue = value;
                _bigIntegerValue = value;
                AuthoritativeType = ScalarType.Int;
                TypeSet = ScalarType.Int;
            }
        }

        public long LongValue
        {
            get => _longValue;
            internal set
            {
                _longValue = value;
                _bigIntegerValue = value;
                AuthoritativeType = ScalarType.Long;
                TypeSet = ScalarType.Long;
            }
        }

        public double DoubleValue
        {
            get => _doubleValue;
            internal set
            {
                _doubleValue = value;
                AuthoritativeType = ScalarType.Double;
                TypeSet = ScalarType.Double;
            }
        }

        public string StringValue
        {
            get => _stringValue;
            internal set
            {
                _stringValue = value;
                AuthoritativeType = ScalarType.String;
                TypeSet = ScalarType.String;
            }
        }

        public BigInteger BigIntegerValue
        {
            get => _bigIntegerValue;
            internal set
            {
                _bigIntegerValue = value;
                AuthoritativeType = ScalarType.BigInteger;
                TypeSet = ScalarType.BigInteger;
            }
        }

        public BigDecimal DecimalValue
        {
            get => _decimalValue;
            internal set
            {
                _decimalValue = value;
//                _doubleValue = Convert.ToDouble(value);
                AuthoritativeType = ScalarType.Decimal;
                TypeSet = ScalarType.Decimal;
            }
        }

        public Timestamp TimestampValue
        {
            get => _timestampValue;
            internal set
            {
                _timestampValue = value;
                AuthoritativeType = ScalarType.Timestamp;
                TypeSet = ScalarType.Timestamp;
            }
        }

        public bool IsEmpty => AuthoritativeType == ScalarType.Nothing;
        public ScalarType TypeSet { get; private set; }
        public ScalarType AuthoritativeType { get; internal set; }

        public void Clear()
        {
            AuthoritativeType = ScalarType.Nothing;
            TypeSet = ScalarType.Nothing;
            _stringValue = null;
        }

        internal void SetNull(IonType ionType)
        {
            switch (ionType)
            {
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
                    AuthoritativeType = ScalarType.Timestamp;
                    break;
                case IonType.Symbol:
                    AuthoritativeType = ScalarType.Int;
                    break;
                case IonType.Float:
                    AuthoritativeType = ScalarType.Double;
                    break;
            }

            TypeSet = ScalarType.Null | AuthoritativeType;
        }

        internal void AddString(string value)
        {
            _stringValue = value;
            TypeSet |= ScalarType.String;
        }

        internal void AddInt(int value)
        {
            _intValue = value;
            _longValue = value;
            _bigIntegerValue = value;
            TypeSet |= ScalarType.Int;
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
