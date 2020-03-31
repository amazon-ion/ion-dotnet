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

namespace Amazon.IonDotnet.Internals.Text
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Numerics;
    using Amazon.IonDotnet.Internals.Conversions;

    internal class SystemTextReader : RawTextReader
    {
        protected readonly ISymbolTable systemSymbols;

        protected SystemTextReader(TextStream input)
            : base(input)
        {
            this.systemSymbols = SharedSymbolTable.GetSystem(1);
        }

        public override bool CurrentIsNull => this.valueVariant.TypeSet.HasFlag(ScalarType.Null);

        public override string CurrentFieldName
        {
            get
            {
                var text = this.fieldName;
                if (text == null && this.fieldNameSid != SymbolToken.UnknownSid)
                {
                    if (this.fieldNameSid != 0 && (text = this.GetSymbolTable().FindKnownSymbol(this.fieldNameSid)) == null)
                    {
                        throw new UnknownSymbolException(this.fieldNameSid);
                    }
                }

                return text;
            }
        }

        public override SymbolToken GetFieldNameSymbol()
        {
            if (this.fieldName is null)
            {
                if (this.fieldNameSid < 0 || this.fieldNameSid > this.GetSymbolTable().MaxId)
                {
                    throw new UnknownSymbolException(this.fieldNameSid);
                }

                this.fieldName = this.GetSymbolTable().FindKnownSymbol(this.fieldNameSid);
            }

            return new SymbolToken(this.fieldName, this.fieldNameSid);
        }

        public override ISymbolTable GetSymbolTable() => this.systemSymbols;

        public override IntegerSize GetIntegerSize()
        {
            this.LoadOnce();
            if (this.valueType != IonType.Int || this.valueVariant.TypeSet.HasFlag(ScalarType.Null))
            {
                return IntegerSize.Unknown;
            }

            return this.valueVariant.IntegerSize;
        }

        public override bool BoolValue()
        {
            if (this.CurrentIsNull)
            {
                throw new NullValueException();
            }

            this.PrepareValue();
            return this.valueVariant.BoolValue;
        }

        public override int IntValue()
        {
            if (this.CurrentIsNull)
            {
                throw new NullValueException();
            }

            this.PrepareValue();
            return this.valueVariant.IntValue;
        }

        public override long LongValue()
        {
            if (this.CurrentIsNull)
            {
                throw new NullValueException();
            }

            this.PrepareValue();
            return this.valueVariant.LongValue;
        }

        public override BigInteger BigIntegerValue()
        {
            if (this.CurrentIsNull)
            {
                throw new NullValueException();
            }

            this.PrepareValue();
            return this.valueVariant.BigIntegerValue;
        }

        public override double DoubleValue()
        {
            if (this.CurrentIsNull)
            {
                throw new NullValueException();
            }

            this.PrepareValue();
            return this.valueVariant.DoubleValue;
        }

        public override BigDecimal DecimalValue()
        {
            if (this.CurrentIsNull)
            {
                throw new NullValueException();
            }

            this.PrepareValue();
            return this.valueVariant.DecimalValue;
        }

        public override Timestamp TimestampValue()
        {
            if (this.CurrentIsNull)
            {
                throw new NullValueException();
            }

            this.PrepareValue();
            return this.valueVariant.TimestampValue;
        }

        public override string StringValue()
        {
            if (!this.valueType.IsText())
            {
                throw new InvalidOperationException($"Value type {this.valueType} is not text");
            }

            this.PrepareValue();
            return this.valueVariant.StringValue;
        }

        public override SymbolToken SymbolValue()
        {
            if (this.valueType != IonType.Symbol)
            {
                throw new InvalidOperationException($"Current value is of type {this.valueType}");
            }

            this.PrepareValue();
            if (this.valueVariant.TypeSet.HasFlag(ScalarType.Int) && !this.valueVariant.TypeSet.HasFlag(ScalarType.String))
            {
                // lookup symbol string from sid
                var text = this.GetSymbolTable().FindKnownSymbol(this.valueVariant.IntValue);
                if (text == null && (this.valueVariant.IntValue > this.GetSymbolTable().MaxId || this.valueVariant.IntValue < 0))
                {
                    throw new UnknownSymbolException(this.valueVariant.IntValue);
                }

                this.valueVariant.AddString(text);
            }
            else if (this.valueVariant.StringValue != null && !this.valueVariant.TypeSet.HasFlag(ScalarType.Int))
            {
                this.valueVariant.AddInt(this.GetSymbolTable().FindSymbolId(this.valueVariant.StringValue));
            }

            return new SymbolToken(this.valueVariant.StringValue, this.valueVariant.IntValue);
        }

        public override int GetBytes(Span<byte> buffer)
        {
            if (!this.valueType.IsLob())
            {
                throw new InvalidOperationException($"Value type {this.valueType} is not a lob");
            }

            this.LoadLobContent();
            if (this.lobValuePosition == this.lobBuffer.Length)
            {
                return 0;
            }

            Span<byte> span = this.lobBuffer;
            var remaining = this.lobBuffer.Length - this.lobValuePosition;
            var bytes = remaining > buffer.Length ? buffer.Length : remaining;

            span.Slice(this.lobValuePosition, bytes).CopyTo(buffer);
            this.lobValuePosition += bytes;
            return bytes;
        }

        public override byte[] NewByteArray()
        {
            if (!this.valueType.IsLob())
            {
                throw new InvalidOperationException($"Value type {this.valueType} is not a lob");
            }

            this.LoadLobContent();
            var newArray = new byte[this.lobBuffer.Length];
            Buffer.BlockCopy(this.lobBuffer, 0, newArray, 0, newArray.Length);
            return newArray;
        }

        public override int GetLobByteSize()
        {
            if (!this.valueType.IsLob())
            {
                throw new InvalidOperationException($"Value type {this.valueType} is not a lob");
            }

            this.LoadLobContent();
            return this.lobBuffer.Length;
        }

        public override string[] GetTypeAnnotations()
        {
            string[] annotations = new string[this.annotations.Count];
            for (int index = 0; index < this.annotations.Count; index++)
            {
                SymbolToken symbolToken = this.annotations[index];
                if (symbolToken.Text is null)
                {
                    if (symbolToken.ImportLocation == default)
                    {
                        throw new UnknownSymbolException(symbolToken.Sid);
                    }
                    else
                    {
                        ISymbolTable symtab = this.GetSymbolTable();

                        string text = symtab.FindKnownSymbol(symbolToken.ImportLocation.Sid);
                        if (text == null)
                        {
                            throw new UnknownSymbolException(symbolToken.ImportLocation.Sid);
                        }

                        annotations[index] = symtab.FindKnownSymbol(symbolToken.ImportLocation.Sid);
                    }
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
            if (this.annotations == null)
            {
                yield break;
            }

            foreach (var a in this.annotations)
            {
                if (a.Text is null && a.ImportLocation != default)
                {
                    var symtab = this.GetSymbolTable();
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
            if (annotation == null)
            {
                throw new ArgumentNullException(nameof(annotation));
            }

            int? annotationId = null;
            foreach (SymbolToken symbolToken in this.annotations)
            {
                if (symbolToken.Text == null)
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

        private void PrepareValue()
        {
            this.LoadOnce();
        }

        private void LoadOnce()
        {
            if (!this.valueVariant.IsEmpty)
            {
                return;
            }

            this.LoadScalarValue();
        }

        private void LoadScalarValue()
        {
            if (!this.valueType.IsScalar())
            {
                return;
            }

            this.LoadTokenContents(this.scanner.Token);
            var negative = false;
            if (this.scanner.Token == TextConstants.TokenHex)
            {
                negative = this.valueBuffer[0] == '-';
                Debug.Assert(this.valueBuffer[negative ? 1 : 0] == '0', "valueBuffer initial value is not 0");
                Debug.Assert(char.ToLower(this.valueBuffer[negative ? 2 : 1]) == 'x', "valueBuffer second value is not x");

                // we need to delete 0x but we also want '0' at the beginning of the hex string
                // so that the .net parsing will work correctly, so we only delete 'x' here (and the leading '+'/'-' if any)
                const int delStart = 1;
                if (this.valueBuffer[0] == '0')
                {
                    // no leading sign
                    this.valueBuffer.Remove(delStart, 1);
                }
                else
                {
                    // leading sign
                    this.valueBuffer[0] = '0';
                    this.valueBuffer.Remove(delStart, 2);
                }
            }
            else if (this.scanner.Token == TextConstants.TokenBinary)
            {
                negative = this.valueBuffer[0] == '-';
                Debug.Assert(this.valueBuffer[negative ? 1 : 0] == '0', "valueBuffer initial value is not 0");
                Debug.Assert(char.ToLower(this.valueBuffer[negative ? 2 : 1]) == 'b', "valueBuffer second value is not b");

                // delete '0b'
                this.valueBuffer.Remove(0, this.valueBuffer[0] != '0' ? 3 : 2);
            }

            var s = this.valueBuffer.ToString();
            this.valueVariant.AddString(s);
            this.ClearValueBuffer();

            switch (this.scanner.Token)
            {
                default:
                    throw new IonException($"Unrecognized token {this.scanner.Token}");
                case TextConstants.TokenUnknownNumeric:
                    switch (this.valueType)
                    {
                        default:
                            throw new IonException($"Expected value type to be numeric, but is {this.valueType}");
                        case IonType.Int:
                            this.SetInteger(Radix.Decimal, s, negative);
                            break;
                        case IonType.Decimal:
                            this.SetDecimalOrDouble(s);
                            break;
                        case IonType.Float:
                            this.SetFloat(s);
                            break;
                        case IonType.Timestamp:
                            this.valueVariant.TimestampValue = Timestamp.Parse(s);
                            break;
                    }

                    break;
                case TextConstants.TokenInt:
                    this.SetInteger(Radix.Decimal, s, negative);
                    break;
                case TextConstants.TokenBinary:
                    this.SetInteger(Radix.Binary, s, negative);
                    break;
                case TextConstants.TokenHex:
                    this.SetInteger(Radix.Hex, s, negative);
                    break;
                case TextConstants.TokenDecimal:
                    this.SetDecimal(s);
                    break;
                case TextConstants.TokenFloat:
                    this.SetFloat(s);
                    break;
                case TextConstants.TokenTimestamp:
                    this.valueVariant.TimestampValue = Timestamp.Parse(s);
                    break;
                case TextConstants.TokenSymbolIdentifier:
                    if (this.CurrentIsNull)
                    {
                        this.valueVariant.SetNull(this.valueType);
                        break;
                    }

                    switch (this.valueType)
                    {
                        default:
                            throw new IonException($"Unexpected type {this.valueType}");
                        case IonType.Symbol:
                            this.valueVariant.StringValue = s;
                            this.valueVariant.AuthoritativeType = ScalarType.String;
                            break;
                        case IonType.Float:
                            if (this.valueKeyword != TextConstants.KeywordNan)
                            {
                                throw new IonException($"Unexpected keyword {s} as float");
                            }

                            this.valueVariant.DoubleValue = double.NaN;
                            break;
                        case IonType.Bool:
                            if (this.valueKeyword == TextConstants.KeywordTrue)
                            {
                                this.valueVariant.BoolValue = true;
                            }
                            else if (this.valueKeyword == TextConstants.KeywordFalse)
                            {
                                this.valueVariant.BoolValue = false;
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
                    this.valueVariant.StringValue = s;
                    break;
                case TextConstants.TokenStringTripleQuote:
                    // long strings (triple quoted strings) are never finished by the raw parser.
                    // At most it reads the first triple quoted string.
                    this.valueVariant.StringValue = s;
                    break;
            }
        }

        /// <summary>
        /// This function tries to set the decimal value of the text, unless it is a float (with 'd') or
        /// the number of decimal places can't hold, then the value is set to 'float'.
        /// </summary>
        /// <param name="text">Number text.</param>
        private void SetDecimalOrDouble(string text)
        {
            foreach (var c in text)
            {
                switch (c)
                {
                    case 'e':
                    case 'E':
                        this.SetFloat(text);
                        return;
                    case 'd':
                    case 'D':
                        this.SetDecimal(text);
                        return;
                }
            }

            var dotIdx = text.IndexOf('.');
            var decimalPlaces = dotIdx < 0 ? 0 : text.Length - dotIdx;
            if (decimalPlaces > 28)
            {
                this.valueVariant.DoubleValue = double.Parse(text, CultureInfo.InvariantCulture);
                this.valueType = IonType.Float;
            }
            else
            {
                this.valueVariant.DecimalValue = BigDecimal.Parse(text);
                this.valueType = IonType.Decimal;
            }
        }

        private void SetFloat(string text)
        {
            try
            {
                var parsed = double.Parse(text, CultureInfo.InvariantCulture);

                // check for negative zero
                if (Math.Abs(parsed) < double.Epsilon * 100 && text[0] == '-')
                {
                    this.valueVariant.DoubleValue = -1.0f * 0;
                }
                else
                {
                    this.valueVariant.DoubleValue = parsed;
                }
            }
            catch (OverflowException)
            {
                this.valueVariant.DoubleValue = text[0] == '-' ? double.NegativeInfinity : double.PositiveInfinity;
            }
        }

        /// <summary>
        /// There is 'd' (decimal token) in the text. This method sets the decimal value.
        /// </summary>
        /// <param name="text">Number text.</param>
        private void SetDecimal(string text)
        {
            this.valueVariant.DecimalValue = BigDecimal.Parse(text);
        }

        private void SetInteger(Radix radix, string s, bool negative)
        {
            var intBase = radix == Radix.Binary ? 2 : (radix == Radix.Decimal ? 10 : 16);

            if (radix.IsInt(s.AsSpan()))
            {
                this.valueVariant.IntValue = negative ? -Convert.ToInt32(s, intBase) : Convert.ToInt32(s, intBase);
                return;
            }

            if (radix.IsLong(s.AsSpan()))
            {
                this.valueVariant.LongValue = negative ? -Convert.ToInt64(s, intBase) : Convert.ToInt64(s, intBase);
                return;
            }

            // bigint
            if (intBase == 10)
            {
                this.valueVariant.BigIntegerValue = negative
                    ? -BigInteger.Parse(s, CultureInfo.InvariantCulture)
                    : BigInteger.Parse(s, CultureInfo.InvariantCulture);
                return;
            }

            if (intBase == 16)
            {
                this.valueVariant.BigIntegerValue = negative
                    ? -BigInteger.Parse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                    : BigInteger.Parse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                return;
            }

            this.SetBigIntegerFromBinaryString(s, negative);
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
                {
                    continue;
                }

                b += 1;
            }

            this.valueVariant.BigIntegerValue = negative ? -b : b;
        }

        private void LoadLobContent()
        {
            Debug.Assert(this.valueType.IsLob(), "valueType is not Lob");

            // check if we already loaded
            if (this.lobBuffer != null)
            {
                return;
            }

            this.ClearValueBuffer();
            switch (this.lobToken)
            {
                default:
                    throw new InvalidTokenException($"Invalid lob format for {this.valueType}");
                case TextConstants.TokenOpenDoubleBrace:
                    this.scanner.LoadBlob(this.valueBuffer);
                    break;
                case TextConstants.TokenStringDoubleQuote:
                    this.scanner.LoadDoubleQuotedString(this.valueBuffer, true);
                    break;
                case TextConstants.TokenStringTripleQuote:
                    this.scanner.LoadTripleQuotedString(this.valueBuffer, true);
                    break;
            }

            if (this.valueType == IonType.Blob)
            {
                this.lobBuffer = Convert.FromBase64String(this.valueBuffer.ToString());
            }
            else
            {
                Array.Resize(ref this.lobBuffer, this.valueBuffer.Length);
                for (int i = 0, l = this.valueBuffer.Length; i < l; i++)
                {
                    this.lobBuffer[i] = (byte)this.valueBuffer[i];
                }
            }

            this.ClearValueBuffer();
        }
    }
}
