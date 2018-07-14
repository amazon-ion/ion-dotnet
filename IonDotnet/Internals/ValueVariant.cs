using System;
using System.Diagnostics;
using System.Numerics;

namespace IonDotnet.Internals
{
    public struct ValueVariant
    {
        [Flags]
        internal enum AsType
        {
            Nothing = 0,
            Null = 1 << 0,
            Bool = 1 << 1,
            Int = 1 << 2,
            Long = 1 << 3,
            BigInteger = 1 << 4,
            Decimal = 1 << 5,
            Double = 1 << 6,
            String = 1 << 7,
            DateTime = 1 << 8
        }

        private AsType _typeSet;
        private AsType _authoritativeType;
        private bool _boolValue;
        private int _intValue;
        private long _longValue;
        private double _doubleValue;
        public string StringValue { get; private set; }
        private BigInteger _bigIntegerValue;
        private decimal _decimalValue;
        private DateTime _datetimeValue;
        //TODO datetime

        public bool IsEmpty => _authoritativeType == AsType.Nothing;

        public void Clear()
        {
            _authoritativeType = AsType.Nothing;
            _typeSet = AsType.Nothing;
        }

        public bool HasNull => _typeSet.HasFlag(AsType.Null);
        public bool HasBool => _typeSet.HasFlag(AsType.Bool);
        public bool HasInt => _typeSet.HasFlag(AsType.Int);
        public bool HasLong => _typeSet.HasFlag(AsType.Long);
        public bool HasBigInteger => _typeSet.HasFlag(AsType.BigInteger);
        public bool HasDecimal => _typeSet.HasFlag(AsType.Decimal);
        public bool HasDouble => _typeSet.HasFlag(AsType.Double);
        public bool HasString => _typeSet.HasFlag(AsType.String);
        public bool HasDateTime => _typeSet.HasFlag(AsType.DateTime);

        internal void SetNull(IonType ionType)
        {
            switch (ionType)
            {
                default:
                    throw new ArgumentOutOfRangeException(nameof(ionType));
                case IonType.Int:
                    _authoritativeType = AsType.Int;
                    break;
                case IonType.Decimal:
                    _authoritativeType = AsType.Decimal;
                    break;
                case IonType.Null:
                    _authoritativeType = AsType.Null;
                    break;
                case IonType.Bool:
                    _authoritativeType = AsType.Bool;
                    break;
                case IonType.String:
                    _authoritativeType = AsType.String;
                    break;
                case IonType.Timestamp:
                    _authoritativeType = AsType.DateTime;
                    break;
                case IonType.Symbol:
                    _authoritativeType = AsType.Int;
                    break;
                case IonType.Float:
                    _authoritativeType = AsType.Double;
                    break;
            }
            _typeSet = AsType.Null;
        }

        internal void AddValue<T>(T value)
        {
            switch (value)
            {
                default:
                    throw new ArgumentOutOfRangeException($"Cannot set type {value.GetType()}");
                case bool boolValue:
                    _boolValue = boolValue;
                    _typeSet |= AsType.Bool;
                    break;
                case int intValue:
                    _intValue = intValue;
                    _typeSet |= AsType.Int;
                    break;
                case long longVal:
                    _longValue = longVal;
                    _typeSet |= AsType.Long;
                    break;
                case BigInteger bigInt:
                    _bigIntegerValue = bigInt;
                    _typeSet |= AsType.BigInteger;
                    break;
                case decimal decimalValue:
                    _decimalValue = decimalValue;
                    _typeSet |= AsType.Decimal;
                    break;
                case double doubleVal:
                    _doubleValue = doubleVal;
                    _typeSet |= AsType.Double;
                    break;
                case string stringVal:
                    StringValue = stringVal;
                    _typeSet |= AsType.String;
                    break;
                case DateTime dateTime:
                    _datetimeValue = dateTime;
                    _typeSet |= AsType.DateTime;
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
                    _boolValue = boolValue;
                    _typeSet = AsType.Bool;
                    _authoritativeType = AsType.Bool;
                    break;
                case int intValue:
                    _intValue = intValue;
                    _typeSet = AsType.Int;
                    _authoritativeType = AsType.Int;
                    break;
                case long longVal:
                    _longValue = longVal;
                    _typeSet = AsType.Long;
                    _authoritativeType = AsType.Long;
                    break;
                case BigInteger bigInt:
                    _bigIntegerValue = bigInt;
                    _typeSet = AsType.BigInteger;
                    _authoritativeType = AsType.BigInteger;
                    break;
                case decimal decimalValue:
                    _decimalValue = decimalValue;
                    _typeSet = AsType.Decimal;
                    _authoritativeType = AsType.Decimal;
                    break;
                case double doubleVal:
                    _doubleValue = doubleVal;
                    _typeSet = AsType.Double;
                    _authoritativeType = AsType.Double;
                    break;
                case string stringVal:
                    StringValue = stringVal;
                    _typeSet = AsType.String;
                    _authoritativeType = AsType.String;
                    break;
                case DateTime dateTime:
                    _datetimeValue = dateTime;
                    _typeSet = AsType.DateTime;
                    _authoritativeType = AsType.DateTime;
                    break;
            }
        }

        public IntegerSize IntegerSize
        {
            get
            {
                switch (_authoritativeType)
                {
                    default:
                        return IntegerSize.Unknown;
                    case AsType.Long:
                        return IntegerSize.Long;
                    case AsType.Int:
                        return IntegerSize.Int;
                    case AsType.BigInteger:
                        return IntegerSize.BigInteger;
                }
            }
        }
    }
}
