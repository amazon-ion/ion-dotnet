using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using IonDotnet.Conversions;

namespace IonDotnet.Internals.Binary
{
    /// <inheritdoc />
    /// <summary>
    /// This class handles the reading and conversion of scalar values (value-type fields)
    /// </summary>
    internal class SystemBinaryReader : RawBinaryReader
    {
        private static readonly BigInteger TwoPow63 = BigInteger.Multiply((long) 1 << 62, 2);

        protected ISymbolTable _symbolTable;
        private readonly IReaderRoutine _readerRoutine;

        internal SystemBinaryReader(Stream input, IReaderRoutine readerRoutine)
            : this(input, SharedSymbolTable.GetSystem(1), readerRoutine)
        {
        }

        private SystemBinaryReader(Stream input, ISymbolTable symboltable, IReaderRoutine readerRoutine) : base(input)
        {
            _symbolTable = symboltable;
            _readerRoutine = readerRoutine;
        }

        private void PrepareValue()
        {
            LoadOnce();
            //we don't allow casting here, so this should end
        }

        protected void LoadOnce()
        {
            //load only once
            if (!_v.IsEmpty) return;
            LoadScalarValue();
        }

        private void LoadScalarValue()
        {
            // make sure we're trying to load a scalar value here
            if (!_valueType.IsScalar()) return;

            if (_valueIsNull)
            {
                _v.SetNull(_valueType);
                return;
            }

            switch (_valueType)
            {
                default:
                    return;
                case IonType.Bool:
                    _v.BoolValue = _valueIsTrue;
                    break;
                case IonType.Int:
                    if (_valueLength == 0)
                    {
                        _v.IntValue = 0;
                        break;
                    }

                    var isNegative = _valueTid == BinaryConstants.TidNegInt;
                    if (_valueLength <= sizeof(long))
                    {
                        //long might be enough
                        var longVal = ReadUlong(_valueLength);
                        if (longVal < 0)
                        {
                            //this might not fit in a long
                            longVal = (longVal << 1) >> 1;
                            var big = BigInteger.Add(TwoPow63, longVal);
                            _v.BigIntegerValue = big;
                        }
                        else
                        {
                            if (isNegative)
                            {
                                longVal = -longVal;
                            }

                            if (longVal < int.MinValue || longVal > int.MaxValue)
                            {
                                _v.LongValue = longVal;
                            }
                            else
                            {
                                _v.IntValue = (int) longVal;
                            }
                        }

                        break;
                    }

                    //here means the int value has to be in bigInt
                    var bigInt = ReadBigInteger(_valueLength, isNegative);
                    _v.BigIntegerValue = bigInt;
                    break;
                case IonType.Float:
                    var d = ReadFloat(_valueLength);
                    _v.DoubleValue = d;
                    break;
                case IonType.Symbol:
                    //treat the symbol as int32, since it's cheap and there's no lookup
                    //until the text is required
                    var sid = ReadUlong(_valueLength);
                    if (sid < 0 || sid > int.MaxValue)
                        throw new IonException("Sid is not an uint32");
                    _v.IntValue = (int) sid;
                    break;
                case IonType.Decimal:
                    _v.DecimalValue = ReadBigDecimal(_valueLength);
                    break;
                case IonType.Timestamp:
                    if (_valueLength == 0)
                    {
                        _v.SetNull(IonType.Timestamp);
                        break;
                    }

                    _v.TimestampValue = ReadTimeStamp(_valueLength);
                    break;
                case IonType.String:
                    _v.StringValue = ReadString(_valueLength);
                    _v.AuthoritativeType = ScalarType.String;
                    break;
            }

            _state = State.AfterValue;
            OnValueEnd();
        }

        /// <summary>
        /// Load the symbol string from the symbol table to _v
        /// </summary>
        /// <remarks>This assumes LoadOnce() has been called and _v already has the sid as Int</remarks>
        /// <exception cref="UnknownSymbolException">The Sid does not exist in the table</exception>
        private void LoadSymbolValue()
        {
            PrepareValue();
            Debug.Assert(_v.TypeSet.HasFlag(ScalarType.Int));
            Debug.Assert(_v.AuthoritativeType == ScalarType.Int, $"AuthType is ${_v.AuthoritativeType}");

            if (_v.TypeSet.HasFlag(ScalarType.String))
                return;

            var text = _symbolTable.FindKnownSymbol(_v.IntValue);
            _v.AddString(text);
        }

        public override BigInteger BigIntegerValue()
        {
            if (_valueIsNull)
                throw new NullValueException();

            PrepareValue();
            return _v.BigIntegerValue;
        }

        public override bool BoolValue()
        {
            if (_valueIsNull)
                throw new NullValueException();
            PrepareValue();
            return _v.BoolValue;
        }

        public override Timestamp TimestampValue()
        {
            if (_valueIsNull)
                throw new NullValueException();
            PrepareValue();
            return _v.TimestampValue;
        }

        public override BigDecimal DecimalValue()
        {
            if (_valueIsNull)
                throw new NullValueException();
            PrepareValue();
            return _v.DecimalValue;
        }

        public override double DoubleValue()
        {
            if (_valueIsNull)
                throw new NullValueException();
            PrepareValue();
            return _v.DoubleValue;
        }

        protected override void OnValueStart() => _readerRoutine?.OnValueStart();

        protected override void OnAnnotation(int annotId)
        {
            if (_readerRoutine == null)
                return;

            var text = _symbolTable.FindKnownSymbol(annotId);
            if (text == null)
                throw new UnknownSymbolException(annotId);
            var token = new SymbolToken(text, annotId);
            _readerRoutine.OnAnnotation(token);
        }

        protected override void OnValueEnd() => _readerRoutine?.OnValueEnd();

        public override string CurrentFieldName
        {
            get
            {
                if (_valueFieldId == SymbolToken.UnknownSid)
                    return null;

                var name = _symbolTable.FindKnownSymbol(_valueFieldId);
                if (name == null)
                    throw new UnknownSymbolException(_valueFieldId);

                return name;
            }
        }

        public override SymbolToken GetFieldNameSymbol()
        {
            if (_valueFieldId == SymbolToken.UnknownSid)
                return default;
            var text = _symbolTable.FindKnownSymbol(_valueFieldId);

            return new SymbolToken(text, _valueFieldId);
        }

        public override IntegerSize GetIntegerSize()
        {
            LoadOnce();
            if (_valueType != IonType.Int || _valueIsNull) return IntegerSize.Unknown;

            return _v.IntegerSize;
        }

        public override ISymbolTable GetSymbolTable() => _symbolTable;

        public override int IntValue()
        {
            if (_valueIsNull)
                throw new NullValueException();
            PrepareValue();
            return _v.IntValue;
        }

        public override long LongValue()
        {
            if (_valueIsNull)
                throw new NullValueException();
            PrepareValue();
            return _v.LongValue;
        }

        public override string StringValue()
        {
            if (!_valueType.IsText())
                throw new InvalidOperationException($"Current value is not text, type {_valueType}");
            if (_valueIsNull)
                return null;
            PrepareValue();

            if (_valueType == IonType.Symbol)
            {
                LoadSymbolValue();
            }

            return _v.StringValue;
        }

        public override SymbolToken SymbolValue()
        {
            if (_valueType != IonType.Symbol)
                throw new InvalidOperationException($"Current value is of type {_valueType}");
            if (_valueIsNull)
                return SymbolToken.None;

            LoadSymbolValue();
            return new SymbolToken(_v.StringValue, _v.IntValue);
        }

        public override bool TryConvertTo(Type targetType, IScalarConverter scalarConverter, out object result)
        {
            PrepareValue();
            return scalarConverter.TryConvertTo(targetType, _v, out result);
        }
    }
}
