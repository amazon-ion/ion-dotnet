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

namespace Amazon.IonDotnet.Internals.Binary
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Numerics;
    using Amazon.IonDotnet.Internals.Conversions;

    /// <inheritdoc />
    /// <summary>
    /// This class handles the reading and conversion of scalar values (value-type fields).
    /// </summary>
    internal class SystemBinaryReader : RawBinaryReader
    {
        internal SystemBinaryReader(Stream input)
            : this(input, SharedSymbolTable.GetSystem(1))
        {
        }

        private SystemBinaryReader(Stream input, ISymbolTable symboltable)
            : base(input)
        {
            this.SymbolTable = symboltable;
        }

        ~SystemBinaryReader()
        {
            this.Dispose(false);
        }

        public override string CurrentFieldName
        {
            get
            {
                if (this.valueFieldId == SymbolToken.UnknownSid)
                {
                    return null;
                }

                var name = this.SymbolTable.FindKnownSymbol(this.valueFieldId);
                if (name == null)
                {
                    throw new UnknownSymbolException(this.valueFieldId);
                }

                return name;
            }
        }

        protected ISymbolTable SymbolTable { get; set; }

        public override BigInteger BigIntegerValue()
        {
            if (this.valueIsNull)
            {
                throw new NullValueException();
            }

            this.PrepareValue();
            return this.valueVariant.BigIntegerValue;
        }

        public override bool BoolValue()
        {
            if (this.valueIsNull)
            {
                throw new NullValueException();
            }

            this.PrepareValue();
            return this.valueVariant.BoolValue;
        }

        public override Timestamp TimestampValue()
        {
            if (this.valueIsNull)
            {
                throw new NullValueException();
            }

            this.PrepareValue();
            return this.valueVariant.TimestampValue;
        }

        public override BigDecimal DecimalValue()
        {
            if (this.valueIsNull)
            {
                throw new NullValueException();
            }

            this.PrepareValue();
            return this.valueVariant.DecimalValue;
        }

        public override double DoubleValue()
        {
            if (this.valueIsNull)
            {
                throw new NullValueException();
            }

            this.PrepareValue();
            return this.valueVariant.DoubleValue;
        }

        public override SymbolToken GetFieldNameSymbol()
        {
            if (this.valueFieldId == SymbolToken.UnknownSid)
            {
                return default;
            }

            var text = this.SymbolTable.FindKnownSymbol(this.valueFieldId);

            return new SymbolToken(text, this.valueFieldId);
        }

        public override IntegerSize GetIntegerSize()
        {
            this.LoadOnce();
            if (this.valueType != IonType.Int || this.valueIsNull)
            {
                return IntegerSize.Unknown;
            }

            return this.valueVariant.IntegerSize;
        }

        public override ISymbolTable GetSymbolTable() => this.SymbolTable;

        public override int IntValue()
        {
            if (this.valueIsNull)
            {
                throw new NullValueException();
            }

            this.PrepareValue();
            return this.valueVariant.IntValue;
        }

        public override long LongValue()
        {
            if (this.valueIsNull)
            {
                throw new NullValueException();
            }

            this.PrepareValue();
            return this.valueVariant.LongValue;
        }

        public override string StringValue()
        {
            if (!this.valueType.IsText())
            {
                throw new InvalidOperationException($"Current value is not text, type {this.valueType}");
            }

            if (this.valueIsNull)
            {
                return null;
            }

            this.PrepareValue();

            if (this.valueType == IonType.Symbol)
            {
                this.LoadSymbolValue();
            }

            return this.valueVariant.StringValue;
        }

        public override SymbolToken SymbolValue()
        {
            if (this.valueType != IonType.Symbol)
            {
                throw new InvalidOperationException($"Current value is of type {this.valueType}");
            }

            if (this.valueIsNull)
            {
                return SymbolToken.None;
            }

            this.LoadSymbolValue();
            return new SymbolToken(this.valueVariant.StringValue, this.valueVariant.IntValue);
        }

        public override string[] GetTypeAnnotations()
        {
            string[] annotations = new string[this.Annotations.Count];
            for (int index = 0; index < this.Annotations.Count; index++)
            {
                string annotation = this.GetSymbolTable().FindKnownSymbol(this.Annotations[index]);
                if (annotation == null)
                {
                    throw new UnknownSymbolException(this.Annotations[index]);
                }

                annotations[index] = this.GetSymbolTable().FindKnownSymbol(this.Annotations[index]);
            }

            return annotations;
        }

        public override IEnumerable<SymbolToken> GetTypeAnnotationSymbols()
        {
            foreach (var aid in this.Annotations)
            {
                var text = this.GetSymbolTable().FindKnownSymbol(aid);

                yield return new SymbolToken(text, aid);
            }
        }

        public override bool HasAnnotation(string annotation)
        {
            if (annotation == null)
            {
                throw new ArgumentNullException(nameof(annotation));
            }

            int? annotationId = null;
            foreach (int aid in this.Annotations)
            {
                string text = this.GetSymbolTable().FindKnownSymbol(aid);
                if (text == null)
                {
                    annotationId = aid;
                }
                else if (annotation.Equals(text))
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

        public override void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.isDisposed)
            {
                return;
            }
            else if (disposing)
            {
                // Intentionally do nothing
                this.isDisposed = true;
            }
        }

        protected void LoadOnce()
        {
            // load only once
            if (!this.valueVariant.IsEmpty)
            {
                return;
            }

            this.LoadScalarValue();
        }

        private void PrepareValue()
        {
            this.LoadOnce();

            // we don't allow casting here, so this should end
        }

        private void LoadScalarValue()
        {
            // make sure we're trying to load a scalar value here
            if (!this.valueType.IsScalar())
            {
                return;
            }

            if (this.valueIsNull)
            {
                this.valueVariant.SetNull(this.valueType);
                return;
            }

            switch (this.valueType)
            {
                default:
                    return;
                case IonType.Bool:
                    this.valueVariant.BoolValue = this.valueIsTrue;
                    break;
                case IonType.Int:
                    if (this.valueLength == 0)
                    {
                        this.valueVariant.IntValue = 0;
                        break;
                    }

                    var isNegative = this.valueTid == BinaryConstants.TidNegInt;
                    if (this.valueLength <= sizeof(long))
                    {
                        // long might be enough
                        var longVal = this.ReadUlong(this.valueLength);
                        if (longVal < 0)
                        {
                            // value wrapped around (overflow), so read it into a BigInteger
                            byte[] magnitude =
                            {
                                (byte)((longVal >> 56) & 0xFF),
                                (byte)((longVal >> 48) & 0xFF),
                                (byte)((longVal >> 40) & 0xFF),
                                (byte)((longVal >> 32) & 0xFF),
                                (byte)((longVal >> 24) & 0xFF),
                                (byte)((longVal >> 16) & 0xFF),
                                (byte)((longVal >> 8) & 0xFF),
                                (byte)(longVal & 0xFF),
                            };
                            this.valueVariant.BigIntegerValue = this.ToBigInteger(magnitude, isNegative);
                        }
                        else
                        {
                            if (isNegative)
                            {
                                longVal = -longVal;
                            }

                            if (longVal < int.MinValue || longVal > int.MaxValue)
                            {
                                this.valueVariant.LongValue = longVal;
                            }
                            else
                            {
                                this.valueVariant.IntValue = (int)longVal;
                            }
                        }

                        break;
                    }

                    // here means the int value has to be in bigInt
                    var bigInt = this.ReadBigInteger(this.valueLength, isNegative);
                    this.valueVariant.BigIntegerValue = bigInt;
                    break;
                case IonType.Float:
                    var d = this.ReadFloat(this.valueLength);
                    this.valueVariant.DoubleValue = d;
                    break;
                case IonType.Symbol:
                    // treat the symbol as int32, since it's cheap and there's no lookup
                    // until the text is required
                    var sid = this.ReadUlong(this.valueLength);
                    if (sid < 0 || sid > int.MaxValue)
                    {
                        throw new IonException("Sid is not an uint32");
                    }

                    this.valueVariant.IntValue = (int)sid;
                    break;
                case IonType.Decimal:
                    this.valueVariant.DecimalValue = this.ReadBigDecimal(this.valueLength);
                    break;
                case IonType.Timestamp:
                    if (this.valueLength == 0)
                    {
                        this.valueVariant.SetNull(IonType.Timestamp);
                        break;
                    }

                    this.valueVariant.TimestampValue = this.ReadTimeStamp(this.valueLength);
                    break;
                case IonType.String:
                    this.valueVariant.StringValue = this.ReadString(this.valueLength);
                    this.valueVariant.AuthoritativeType = ScalarType.String;
                    break;
            }

            this.state = State.AfterValue;
        }

        /// <summary>
        /// Load the symbol string from the symbol table to valueVariant.
        /// </summary>
        /// <remarks>This assumes LoadOnce() has been called and valueVariant already has the sid as Int.</remarks>
        /// <exception cref="UnknownSymbolException">The Sid does not exist in the table.</exception>
        private void LoadSymbolValue()
        {
            this.PrepareValue();
            Debug.Assert(this.valueVariant.TypeSet.HasFlag(ScalarType.Int), "Flag is not Int");
            Debug.Assert(this.valueVariant.AuthoritativeType == ScalarType.Int, $"AuthType is ${this.valueVariant.AuthoritativeType}");

            if (this.valueVariant.TypeSet.HasFlag(ScalarType.String))
            {
                return;
            }

            var text = this.SymbolTable.FindKnownSymbol(this.valueVariant.IntValue);
            this.valueVariant.AddString(text);
        }
    }
}
