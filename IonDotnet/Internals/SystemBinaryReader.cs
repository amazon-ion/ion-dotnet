using System;
using System.IO;
using System.Numerics;
using static IonDotnet.Internals.ValueVariant;

namespace IonDotnet.Internals
{
    /// <summary>
    /// This class handles the reading and conversion of scalar values (value-type fields)
    /// </summary>
    internal class SystemBinaryReader : RawBinaryReader
    {
        private static readonly BigInteger TwoPow63 = BigInteger.Multiply((long)1 << 62, 2);

        protected ISymbolTable _symbolTable;
        private IScalarConverter _scalarConverter;

        internal SystemBinaryReader(Stream input, IScalarConverter scalarConverter)
            : this(input, SharedSymbolTable.GetSystem(1), scalarConverter)
        {
        }

        protected SystemBinaryReader(Stream input, ISymbolTable symboltable, IScalarConverter scalarConverter) : base(input)
        {
            _symbolTable = symboltable;
            _scalarConverter = scalarConverter;
        }

        private void PrepareValue()
        {
            LoadOnce();
            //TODO stuffs with annotations
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
                    _v.SetValue(_valueIsTrue);
                    break;
                case IonType.Int:
                    if (_valueLength == 0)
                    {
                        _v.SetValue(0);
                        break;
                    }
                    var isNegative = _valueTid == IonConstants.TidNegInt;
                    if (_valueLength < sizeof(long))
                    {
                        //long might be enough
                        var longVal = ReadLong(_valueLength);
                        if (longVal < 0)
                        {
                            //this might not fit in a long
                            longVal = (longVal << 1) >> 1;
                            var big = BigInteger.Add(TwoPow63, longVal);
                            _v.SetValue(big);
                        }
                        else
                        {
                            if (isNegative)
                            {
                                longVal = -longVal;
                            }
                            if (longVal < int.MinValue || longVal > int.MaxValue)
                            {
                                _v.SetValue(longVal);
                            }
                            else
                            {
                                _v.SetValue((int)longVal);
                            }
                        }
                        break;
                    }
                    //here means the int value has to be in bigInt
                    var bigInt = ReadBigInteger(_valueLength, isNegative);
                    _v.SetValue(bigInt);
                    break;
                case IonType.Float:
                    var d = ReadFloat(_valueLength);
                    _v.SetValue(d);
                    break;
                case IonType.Decimal:
                case IonType.Timestamp:
                case IonType.Symbol:
                    throw new NotImplementedException();
                case IonType.String:
                    var s = ReadString(_valueLength);
                    _v.SetValue(s);
                    break;
            }
            _state = State.AfterValue;
        }

        public override BigInteger BigIntegerValue()
        {
            if (!_valueType.IsNumeric()) throw new InvalidOperationException($"Current value is not numeric, type {_valueType}");
            if (_valueIsNull) throw new NullValueException();

            PrepareValue();
            return _scalarConverter.ToBigInteger(_v, _symbolTable);
        }

        public override bool BoolValue()
        {
            PrepareValue();
            return _scalarConverter.ToBool(_v, _symbolTable);
        }

        public override DateTime DateTimeValue()
        {
            PrepareValue();
            return _scalarConverter.ToDateTime(_v, _symbolTable);
        }

        public override decimal DecimalValue()
        {
            PrepareValue();
            return _scalarConverter.ToDecimal(_v, _symbolTable);
        }

        public override double DoubleValue()
        {
            PrepareValue();
            return _scalarConverter.ToDouble(_v, _symbolTable);
        }

        public override string GetFieldName()
        {
            if (_valueFieldId == SymbolToken.UnknownSid) return null;

            var name = _symbolTable.FindKnownSymbol(_valueFieldId);
            if (name == null) throw new UnknownSymbolException(_valueFieldId);

            return name;
        }

        public override SymbolToken GetFieldNameSymbol()
        {
            if (_valueFieldId == SymbolToken.UnknownSid) return SymbolToken.None;
            var text = _symbolTable.FindKnownSymbol(_valueFieldId);
            if (text == null) return SymbolToken.None;

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
            if (!_valueType.IsNumeric()) throw new InvalidOperationException($"Current value is not numeric, type {_valueType}");

            PrepareValue();
            return _scalarConverter.ToInt(_v, _symbolTable);
        }

        public override long LongValue()
        {
            if (!_valueType.IsNumeric()) throw new InvalidOperationException($"Current value is not numeric, type {_valueType}");

            PrepareValue();
            return _scalarConverter.ToLong(_v, _symbolTable);
        }

        public override string StringValue()
        {
            if (!_valueType.IsText()) throw new InvalidOperationException($"Current value is not text, type {_valueType}");
            if (_valueIsNull) return null;

            if (_valueType == IonType.Symbol)
            {
                //TODO symbols stuff
            }
            else
            {
                PrepareValue();
            }
            return _v.StringValue;
        }

        public override SymbolToken SymbolValue()
        {
            if (_valueType != IonType.Symbol) throw new InvalidOperationException($"Current value is of type {_valueType}");
            if (_valueIsNull) return SymbolToken.None;

            throw new NotImplementedException();
        }
    }
}
