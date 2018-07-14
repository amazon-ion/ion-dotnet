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
        private ValueVariant _v;

        internal SystemBinaryReader(Stream input) : this(input, SharedSymbolTable.GetSystem(1))
        {
        }

        protected SystemBinaryReader(Stream input, ISymbolTable symboltable) : base(input)
        {
            _symbolTable = symboltable;
        }

        private void LoadOnce()
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
                        var longVal = ReadUInt64(_valueLength);
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
            }
        }

        public override BigInteger BigIntegerValue()
        {
            throw new NotImplementedException();
        }

        public override bool BoolValue()
        {
            throw new NotImplementedException();
        }

        public override DateTime DateTimeValue()
        {
            throw new NotImplementedException();
        }

        public override decimal DecimalValue()
        {
            throw new NotImplementedException();
        }

        public override double DoubleValue()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public override ISymbolTable GetSymbolTable()
        {
            throw new NotImplementedException();
        }

        public override int IntValue()
        {
            throw new NotImplementedException();
        }

        public override long LongValue()
        {
            throw new NotImplementedException();
        }

        public override string StringValue()
        {
            throw new NotImplementedException();
        }

        public override SymbolToken SymbolValue()
        {
            if (_valueType != IonType.Symbol) throw new InvalidOperationException($"Current value is of type {_valueType}");
            if (_valueIsNull) return SymbolToken.None;

            throw new NotImplementedException();
        }
    }
}
