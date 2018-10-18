using System;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using IonDotnet.Conversions;

namespace IonDotnet.Internals.Text
{
    internal class SystemTextReader : RawTextReader
    {
        protected readonly ISymbolTable _systemSymbols;

        protected SystemTextReader(TextStream input) : base(input)
        {
            _systemSymbols = SharedSymbolTable.GetSystem(1);
        }

        private void PrepareValue()
        {
            LoadOnce();
        }

        private void LoadOnce()
        {
            if (!_v.IsEmpty)
                return;
            LoadScalarValue();
        }

        private void LoadScalarValue()
        {
            if (!_valueType.IsScalar())
                return;

            LoadTokenContents(_scanner.Token);
            var negative = false;
            if (_scanner.Token == TextConstants.TokenHex)
            {
                negative = _valueBuffer[0] == '-';
                Debug.Assert(_valueBuffer[negative ? 1 : 0] == '0');
                Debug.Assert(char.ToLower(_valueBuffer[negative ? 2 : 1]) == 'x');

                //delete '0x'
                //TODO is there a better way?
                _valueBuffer.Remove(0, negative ? 3 : 2);
            }
            else if (_scanner.Token == TextConstants.TokenBinary)
            {
                negative = _valueBuffer[0] == '-';
                Debug.Assert(_valueBuffer[negative ? 1 : 0] == '0');
                Debug.Assert(char.ToLower(_valueBuffer[negative ? 2 : 1]) == 'b');
                //delete '0b'
                //TODO is there a better way?
                _valueBuffer.Remove(0, negative ? 3 : 2);
            }

            //TODO is there a better way
            var s = _valueBuffer.ToString();
            _v.AddString(s);
            ClearValueBuffer();

            switch (_scanner.Token)
            {
                default:
                    throw new IonException($"Unrecognized token {_scanner.Token}");
                case TextConstants.TokenUnknownNumeric:
                    switch (_valueType)
                    {
                        default:
                            throw new IonException($"Expected value type to be numeric, but is {_valueType}");
                        case IonType.Int:
                            SetInteger(Radix.Decimal, s, negative);
                            break;
                        case IonType.Decimal:
                            SetDecimalOrDouble(s);
                            break;
                        case IonType.Float:
                            SetFloat(s);
                            break;
                        case IonType.Timestamp:
                            _v.TimestampValue = Timestamp.Parse(s);
                            break;
                    }

                    break;
                case TextConstants.TokenInt:
                    SetInteger(Radix.Decimal, s, negative);
                    break;
                case TextConstants.TokenBinary:
                    SetInteger(Radix.Binary, s, negative);
                    break;
                case TextConstants.TokenHex:
                    SetInteger(Radix.Hex, s, negative);
                    break;
                case TextConstants.TokenDecimal:
                    SetDecimal(s);
                    break;
                case TextConstants.TokenFloat:
                    SetFloat(s);
                    break;
                case TextConstants.TokenTimestamp:
                    _v.TimestampValue = Timestamp.Parse(s);
                    break;
                case TextConstants.TokenSymbolIdentifier:
                    if (CurrentIsNull)
                    {
                        _v.SetNull(_valueType);
                        break;
                    }

                    switch (_valueType)
                    {
                        default:
                            throw new IonException($"Unexpected type {_valueType}");
                        case IonType.Symbol:
                            _v.StringValue = s;
                            _v.AuthoritativeType = ScalarType.String;
                            break;
                        case IonType.Float:
                            if (_valueKeyword != TextConstants.KeywordNan)
                                throw new IonException($"Unexpected keyword {s} as float");
                            _v.DoubleValue = double.NaN;
                            break;
                        case IonType.Bool:
                            if (_valueKeyword == TextConstants.KeywordTrue)
                            {
                                _v.BoolValue = true;
                            }
                            else if (_valueKeyword == TextConstants.KeywordFalse)
                            {
                                _v.BoolValue = false;
                            }
                            else
                            {
                                throw new IonException($"Unexpected keyword {s} as bool");
                            }

                            break;
                    }

                    break;
                case TextConstants.TokenSymbolQuoted:
                case TextConstants.TokenSymbolOperator:
                case TextConstants.TokenStringDoubleQuote:
                    _v.StringValue = s;
                    break;
                case TextConstants.TokenStringTripleQuote:
                    // long strings (triple quoted strings) are never finished by the raw parser.
                    // At most it reads the first triple quoted string.
                    _v.StringValue = s;
                    break;
            }
        }

        /// <summary>
        /// This function tries to set the decimal value of the text, unless it is a float (with 'd') or
        /// the number of decimal places can't hold, then the value is set to 'float'.
        /// </summary>
        /// <param name="text">Number text</param>
        private void SetDecimalOrDouble(string text)
        {
            foreach (var c in text)
            {
                switch (c)
                {
                    case 'e':
                    case 'E':
                        SetFloat(text);
                        return;
                    case 'd':
                    case 'D':
                        SetDecimal(text);
                        return;
                }
            }

            var dotIdx = text.IndexOf('.');
            var decimalPlaces = dotIdx < 0 ? 0 : text.Length - dotIdx;
            if (decimalPlaces > 28)
            {
                _v.DoubleValue = double.Parse(text, CultureInfo.InvariantCulture);
                _valueType = IonType.Float;
            }
            else
            {
                _v.DecimalValue = BigDecimal.Parse(text);
                _valueType = IonType.Decimal;
            }
        }

        private void SetFloat(string text)
        {
            try
            {
                _v.DoubleValue = double.Parse(text, CultureInfo.InvariantCulture);
            }
            catch (OverflowException)
            {
                _v.DoubleValue = text[0] == '-' ? double.NegativeInfinity : double.PositiveInfinity;
            }
        }

        /// <summary>
        /// There is 'd' (decimal token) in the text. This method sets the decimal value.
        /// </summary>
        /// <param name="text">Number text</param>
        private void SetDecimal(string text)
        {
            _v.DecimalValue = BigDecimal.Parse(text);
        }

        private void SetInteger(Radix radix, string s, bool negative)
        {
            var intBase = radix == Radix.Binary ? 2 : (radix == Radix.Decimal ? 10 : 16);

            if (radix.IsInt(s.AsSpan()))
            {
                _v.IntValue = negative ? -Convert.ToInt32(s, intBase) : Convert.ToInt32(s, intBase);
                return;
            }

            if (radix.IsLong(s.AsSpan()))
            {
                _v.LongValue = negative ? -Convert.ToInt64(s, intBase) : Convert.ToInt64(s, intBase);
                return;
            }

            //bigint
            if (intBase == 10)
            {
                _v.BigIntegerValue = negative
                    ? -BigInteger.Parse(s, CultureInfo.InvariantCulture)
                    : BigInteger.Parse(s, CultureInfo.InvariantCulture);
                return;
            }

            if (intBase == 16)
            {
                _v.BigIntegerValue = negative
                    ? -BigInteger.Parse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                    : BigInteger.Parse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                return;
            }

            //does anyone really do this?
            SetBigIntegerFromBinaryString(s, negative);
        }

        public override string CurrentFieldName
        {
            get
            {
                //TODO embedded document?
                var text = _fieldName;
                if (text == null && _fieldNameSid != SymbolToken.UnknownSid)
                {
                    if (_fieldNameSid != 0 && (text = GetSymbolTable().FindKnownSymbol(_fieldNameSid)) == null)
                        throw new UnknownSymbolException(_fieldNameSid);
                }

                return text;
            }
        }

        public override SymbolToken GetFieldNameSymbol() => new SymbolToken(CurrentFieldName, _fieldNameSid);

        private void SetBigIntegerFromBinaryString(string s, bool negative)
        {
            var b = BigInteger.Zero;
            var start = 0;

            while (start < s.Length && s[start++] != 0)
            {
            }

            for (var i = s.Length - 1; i >= start; i--)
            {
                b = BigInteger.Multiply(b, 2);
                if (s[i] == '0')
                    continue;

                b += 1;
            }

            _v.BigIntegerValue = negative ? -b : b;
        }

        private void LoadLobContent()
        {
            //check if we already loaded
            if (_lobBuffer != null)
                return;

            //TODO handle other types of lob content
            switch (_lobToken)
            {
                case TextConstants.TokenOpenDoubleBrace:
                    ClearValueBuffer();
                    _scanner.LoadBlob(_valueBuffer);
                    break;
            }

            //TODO this is horrible but does it matter?
            _lobBuffer = Convert.FromBase64String(_valueBuffer.ToString());
            ClearValueBuffer();
        }

        public override ISymbolTable GetSymbolTable() => _systemSymbols;

        public override IntegerSize GetIntegerSize()
        {
            LoadOnce();
            if (_valueType != IonType.Int || _v.TypeSet.HasFlag(ScalarType.Null))
                return IntegerSize.Unknown;

            return _v.IntegerSize;
        }

        public override bool CurrentIsNull => _v.TypeSet.HasFlag(ScalarType.Null);

        public override bool BoolValue()
        {
            if (CurrentIsNull)
                throw new NullValueException();

            PrepareValue();
            return _v.BoolValue;
        }

        public override int IntValue()
        {
            if (CurrentIsNull)
                throw new NullValueException();

            PrepareValue();
            return _v.IntValue;
        }

        public override long LongValue()
        {
            if (CurrentIsNull)
                throw new NullValueException();

            PrepareValue();
            return _v.LongValue;
        }

        public override BigInteger BigIntegerValue()
        {
            if (CurrentIsNull)
                throw new NullValueException();

            PrepareValue();
            return _v.BigIntegerValue;
        }

        public override double DoubleValue()
        {
            if (CurrentIsNull)
                throw new NullValueException();

            PrepareValue();
            return _v.DoubleValue;
        }

        public override BigDecimal DecimalValue()
        {
            if (CurrentIsNull)
                throw new NullValueException();

            PrepareValue();
            return _v.DecimalValue;
        }

        public override Timestamp TimestampValue()
        {
            if (CurrentIsNull)
                throw new NullValueException();

            PrepareValue();
            return _v.TimestampValue;
        }

        public override string StringValue()
        {
            if (!_valueType.IsText())
                throw new InvalidOperationException($"Value type {_valueType} is not text");

            PrepareValue();
            return _v.StringValue;
        }

        public override SymbolToken SymbolValue()
        {
            if (_valueType != IonType.Symbol)
                throw new InvalidOperationException($"Current value is of type {_valueType}");

            PrepareValue();
            if (_v.TypeSet.HasFlag(ScalarType.Int) && !_v.TypeSet.HasFlag(ScalarType.String))
            {
                //lookup symbol string from sid
                _v.AddString(GetSymbolTable().FindKnownSymbol(_v.IntValue));
            }
            else if (_v.StringValue != null && !_v.TypeSet.HasFlag(ScalarType.Int))
            {
                _v.AddInt(GetSymbolTable().FindSymbolId(_v.StringValue));
            }

            return new SymbolToken(_v.StringValue, _v.IntValue);
        }

        public override int GetBytes(Span<byte> buffer)
        {
            LoadLobContent();
            if (_lobValuePosition == _lobBuffer.Length)
                return 0;

            Span<byte> span = _lobBuffer;
            var remaining = _lobBuffer.Length - _lobValuePosition;
            var bytes = remaining > buffer.Length ? buffer.Length : remaining;

            span.Slice(_lobValuePosition, bytes).CopyTo(buffer);
            _lobValuePosition += bytes;
            return bytes;
        }

        public override bool TryConvertTo(Type targetType, IScalarConverter scalarConverter, out object result)
        {
            PrepareValue();
            return scalarConverter.TryConvertTo(targetType, _v, out result);
        }

        public override byte[] NewByteArray()
        {
            LoadLobContent();
            var newArray = new byte[_lobBuffer.Length];
            Buffer.BlockCopy(_lobBuffer, 0, newArray, 0, newArray.Length);
            return newArray;
        }

        public override int GetLobByteSize()
        {
            LoadLobContent();
            return _lobBuffer.Length;
        }
    }
}
