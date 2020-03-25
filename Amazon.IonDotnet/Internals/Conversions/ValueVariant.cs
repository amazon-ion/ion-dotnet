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

namespace Amazon.IonDotnet.Internals.Conversions
{
    using System.Numerics;

    /// <summary>
    /// Structure that holds the loaded scalar value from IonReader.
    /// </summary>
    internal struct ValueVariant
    {
        private int intValue;
        private long longValue;
        private double doubleValue;
        private string stringValue;
        private BigInteger bigIntegerValue;
        private BigDecimal decimalValue;
        private Timestamp timestampValue;
        private bool boolValue;

        public bool BoolValue
        {
            get => this.boolValue;
            internal set
            {
                this.boolValue = value;
                this.AuthoritativeType = ScalarType.Bool;
                this.TypeSet = ScalarType.Bool;
            }
        }

        public int IntValue
        {
            get => this.intValue;
            internal set
            {
                this.intValue = value;
                this.longValue = value;
                this.bigIntegerValue = value;
                this.AuthoritativeType = ScalarType.Int;
                this.TypeSet = ScalarType.Int;
            }
        }

        public long LongValue
        {
            get => this.longValue;
            internal set
            {
                this.longValue = value;
                this.bigIntegerValue = value;
                this.AuthoritativeType = ScalarType.Long;
                this.TypeSet = ScalarType.Long;
            }
        }

        public double DoubleValue
        {
            get => this.doubleValue;
            internal set
            {
                this.doubleValue = value;
                this.AuthoritativeType = ScalarType.Double;
                this.TypeSet = ScalarType.Double;
            }
        }

        public string StringValue
        {
            get => this.stringValue;
            internal set
            {
                this.stringValue = value;
                this.AuthoritativeType = ScalarType.String;
                this.TypeSet = ScalarType.String;
            }
        }

        public BigInteger BigIntegerValue
        {
            get => this.bigIntegerValue;
            internal set
            {
                this.bigIntegerValue = value;
                this.AuthoritativeType = ScalarType.BigInteger;
                this.TypeSet = ScalarType.BigInteger;
            }
        }

        public BigDecimal DecimalValue
        {
            get => this.decimalValue;
            internal set
            {
                this.decimalValue = value;
                this.AuthoritativeType = ScalarType.Decimal;
                this.TypeSet = ScalarType.Decimal;
            }
        }

        public Timestamp TimestampValue
        {
            get => this.timestampValue;
            internal set
            {
                this.timestampValue = value;
                this.AuthoritativeType = ScalarType.Timestamp;
                this.TypeSet = ScalarType.Timestamp;
            }
        }

        public bool IsEmpty => this.AuthoritativeType == ScalarType.Nothing;

        public ScalarType TypeSet { get; private set; }

        public ScalarType AuthoritativeType { get; internal set; }

        public IntegerSize IntegerSize
        {
            get
            {
                switch (this.AuthoritativeType)
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

        public void Clear()
        {
            this.AuthoritativeType = ScalarType.Nothing;
            this.TypeSet = ScalarType.Nothing;
            this.stringValue = null;
        }

        internal void SetNull(IonType ionType)
        {
            switch (ionType)
            {
                case IonType.Int:
                    this.AuthoritativeType = ScalarType.Int;
                    break;
                case IonType.Decimal:
                    this.AuthoritativeType = ScalarType.Decimal;
                    break;
                case IonType.Null:
                    this.AuthoritativeType = ScalarType.Null;
                    break;
                case IonType.Bool:
                    this.AuthoritativeType = ScalarType.Bool;
                    break;
                case IonType.String:
                    this.AuthoritativeType = ScalarType.String;
                    break;
                case IonType.Timestamp:
                    this.AuthoritativeType = ScalarType.Timestamp;
                    break;
                case IonType.Symbol:
                    this.AuthoritativeType = ScalarType.Int;
                    break;
                case IonType.Float:
                    this.AuthoritativeType = ScalarType.Double;
                    break;
            }

            this.TypeSet = ScalarType.Null | this.AuthoritativeType;
        }

        internal void AddString(string value)
        {
            this.stringValue = value;
            this.TypeSet |= ScalarType.String;
        }

        internal void AddInt(int value)
        {
            this.intValue = value;
            this.longValue = value;
            this.bigIntegerValue = value;
            this.TypeSet |= ScalarType.Int;
        }
    }
}
