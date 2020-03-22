/*
 * Copyright 2020 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using Amazon.IonDotnet.Internals.Conversions;

namespace Amazon.IonDotnet.Internals.Text
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

                //we need to delete 0x but we also want '0' at the beginning of the hex string
                //so that the .net parsing will work correctly, so we only delete 'x' here (and the leading '+'/'-' if any)
                const int delStart = 1;
                if (_valueBuffer[0] == '0')
                {
                    //no leading sign
                    _valueBuffer.Remove(delStart, 1);
                }
                else
                {
                    //leading sign
                    _valueBuffer[0] = '0';
                    _valueBuffer.Remove(delStart, 2);
                }
            }
            else if (_scanner.Token == TextConstants.TokenBinary)
            {
                negative = _valueBuffer[0] == '-';
                Debug.Assert(_valueBuffer[negative ? 1 : 0] == '0');
                Debug.Assert(char.ToLower(_valueBuffer[negative ? 2 : 1]) == 'b');
                //delete '0b'
                //TODO is there a better way?
                _valueBuffer.Remove(0, _valueBuffer[0] != '0' ? 3 : 2);
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
                var parsed = double.Parse(text, CultureInfo.InvariantCulture);
                //check for negative zero
                if (Math.Abs(parsed) < double.Epsilon * 100 && text[0] == '-')
                {
                    _v.DoubleValue = -1.0f * 0;
                }
                else
                {
                    _v.DoubleValue = parsed;
                }
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

        public override SymbolToken GetFieldNameSymbol()
        {
            if (_fieldName is null)
            {
                if (_fieldNameSid < 0 || _fieldNameSid > GetSymbolTable().MaxId)
                    throw new UnknownSymbolException(_fieldNameSid);

                _fieldName = GetSymbolTable().FindKnownSymbol(_fieldNameSid);
            }

            return new SymbolToken(_fieldName, _fieldNameSid);
        }

        private void SetBigIntegerFromBinaryString(string s, bool negative)
        {
            var b = BigInteger.Zero;
            var start = 0;

            while (start < s.Length && s[start++] != 0)
            {
            }

            for (var i = s.Length - 1; i >= start; i--)
            {
                b <<= 1;
                if (s[i] == '0')
                    continue;

                b += 1;
            }

            _v.BigIntegerValue = negative ? -b : b;
        }

        private void LoadLobContent()
        {
            Debug.Assert(_valueType.IsLob());

            //check if we already loaded
            if (_lobBuffer != null)
                return;

            ClearValueBuffer();
            switch (_lobToken)
            {
                default:
                    throw new InvalidTokenException($"Invalid lob format for {_valueType}");
                case TextConstants.TokenOpenDoubleBrace:
                    _scanner.LoadBlob(_valueBuffer);
                    break;
                case TextConstants.TokenStringDoubleQuote:
                    _scanner.LoadDoubleQuotedString(_valueBuffer, true);
                    break;
                case TextConstants.TokenStringTripleQuote:
                    _scanner.LoadTripleQuotedString(_valueBuffer, true);
                    break;
            }

            //TODO this is horrible but does it matter?
            if (_valueType == IonType.Blob)
            {
                _lobBuffer = Convert.FromBase64String(_valueBuffer.ToString());
            }
            else
            {
                Array.Resize(ref _lobBuffer, _valueBuffer.Length);
                for (int i = 0, l = _valueBuffer.Length; i < l; i++)
                {
                    _lobBuffer[i] = (byte) _valueBuffer[i];
                }
            }

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
                var text = GetSymbolTable().FindKnownSymbol(_v.IntValue);
                if (text == null && (_v.IntValue > GetSymbolTable().MaxId || _v.IntValue < 0))
                {
                    throw new UnknownSymbolException(_v.IntValue);
                }
                _v.AddString(text);
            }
            else if (_v.StringValue != null && !_v.TypeSet.HasFlag(ScalarType.Int))
            {
                _v.AddInt(GetSymbolTable().FindSymbolId(_v.StringValue));
            }

            return new SymbolToken(_v.StringValue, _v.IntValue);
        }

        public override int GetBytes(Span<byte> buffer)
        {
            if (!_valueType.IsLob())
                throw new InvalidOperationException($"Value type {_valueType} is not a lob");

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

        public override byte[] NewByteArray()
        {
            if (!_valueType.IsLob())
                throw new InvalidOperationException($"Value type {_valueType} is not a lob");

            LoadLobContent();
            var newArray = new byte[_lobBuffer.Length];
            Buffer.BlockCopy(_lobBuffer, 0, newArray, 0, newArray.Length);
            return newArray;
        }

        public override int GetLobByteSize()
        {
            if (!_valueType.IsLob())
                throw new InvalidOperationException($"Value type {_valueType} is not a lob");

            LoadLobContent();
            return _lobBuffer.Length;
        }

        public override string[] GetTypeAnnotations()
        {
            string[] annotations = new string[_annotations.Count];
            for (int index = 0; index < _annotations.Count; index++)
            {
                SymbolToken symbolToken = _annotations[index];
                if (symbolToken.Text is null && symbolToken.ImportLocation != default)
                {
                    ISymbolTable symtab = GetSymbolTable();

                    string text = symtab.FindKnownSymbol(symbolToken.ImportLocation.Sid);
                    if (text == null && symbolToken.ImportLocation.Sid != 0)
                    {
                        throw new UnknownSymbolException(symbolToken.ImportLocation.Sid);
                    }

                    annotations[index] = symtab.FindKnownSymbol(symbolToken.ImportLocation.Sid);
                }
                else
                {
                    annotations[index] = symbolToken.Text;
                }
            }

            return annotations;
        }

        public override IEnumerable<SymbolToken> GetTypeAnnotationSymbols()
        {
            if (_annotations == null)
            {
                yield break;
            }

            foreach (var a in _annotations)
            {
                if (a.Text is null && a.ImportLocation != default)
                {
                    var symtab = GetSymbolTable();
                    if (a.ImportLocation.Sid < -1 || a.ImportLocation.Sid > symtab.MaxId)
                    {
                        throw new UnknownSymbolException(a.Sid);
                    }

                    var text = symtab.FindKnownSymbol(a.ImportLocation.Sid);
                    yield return new SymbolToken(text, a.Sid, a.ImportLocation);
                }
                else
                {
                    yield return a;
                }
            }
        }

        public override bool HasAnnotation(string annotation)
        {
            int? annotationId = null;
            foreach (SymbolToken symbolToken in _annotations)
            {
                //zero symbol scenario
                if (annotation == null && symbolToken.Text == null && symbolToken.Sid == 0)
                {
                    return true;
                }
                else if (symbolToken.Text == null)
                {
                    annotationId = symbolToken.Sid;
                }
                else if (annotation.Equals(symbolToken.Text))
                {
                    return true;
                }
            }

            if (annotationId.HasValue)
            {
                throw new UnknownSymbolException(annotationId.Value);
            }

            return false;
        }
    }
}
