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

namespace Amazon.IonDotnet.Tree.Impl
{
    using System;
    using System.Diagnostics;
    using System.Numerics;
    using Amazon.IonDotnet.Internals;

    internal sealed class IonInt : IonValue, IIonInt
    {
        // This mask combine the 4th and 5th bit of the flag byte
        private const int IntSizeMask = 0x18;

        // Right shift 3 bits to get the value
        private const int IntSizeShift = 3;

        private long longValue;
        private BigInteger? bigInteger;

        public IonInt(long value)
            : base(false)
        {
            this.SetLongValue(value);
        }

        public IonInt(BigInteger value)
            : base(false)
        {
            this.SetBigIntegerValue(value);
        }

        private IonInt(bool isNull)
            : base(isNull)
        {
        }

        public override int IntValue
        {
            get
            {
                this.ThrowIfNull();
                if (this.IntegerSize != IntegerSize.Int)
                {
                    throw new OverflowException($"Size of this {nameof(IonInt)} is {this.IntegerSize}");
                }

                return (int)this.longValue;
            }
        }

        public override long LongValue
        {
            get
            {
                this.ThrowIfNull();
                if (this.bigInteger != null)
                {
                    throw new OverflowException($"Size of this {nameof(IonInt)} is {this.IntegerSize}");
                }

                return this.longValue;
            }
        }

        public override BigInteger BigIntegerValue
        {
            get
            {
                this.ThrowIfNull();
                return this.bigInteger ?? new BigInteger(this.longValue);
            }
        }

        public override IntegerSize IntegerSize
        {
            get
            {
                if (this.NullFlagOn())
                {
                    return IntegerSize.Unknown;
                }

                var metadata = this.GetMetadata(IntSizeMask, IntSizeShift);
                Debug.Assert(metadata < 3, "metadata is not less than 3");
                return (IntegerSize)metadata;
            }
        }

        /// <summary>
        /// Returns a new null.int value.
        /// </summary>
        /// <returns>A null IonInt.</returns>
        public static IonInt NewNull() => new IonInt(true);

        public override bool IsEquivalentTo(IIonValue other)
        {
            if (!base.IsEquivalentTo(other))
            {
                return false;
            }

            if (!(other is IonInt oInt))
            {
                return false;
            }

            if (this.NullFlagOn())
            {
                return oInt.IsNull;
            }

            if (oInt.IsNull)
            {
                return false;
            }

            if (oInt.IntegerSize != this.IntegerSize)
            {
                return false;
            }

            switch (this.IntegerSize)
            {
                case IntegerSize.Int:
                    return this.IntValue == oInt.IntValue;
                case IntegerSize.Long:
                    return this.LongValue == oInt.LongValue;
                case IntegerSize.BigInteger:
                    return this.BigIntegerValue == oInt.BigIntegerValue;
                default:
                    return false;
            }
        }

        public override IonType Type() => IonType.Int;

        internal override void WriteBodyTo(IPrivateWriter writer)
        {
            if (this.NullFlagOn())
            {
                writer.WriteNull(IonType.Int);
                return;
            }

            if (this.bigInteger != null)
            {
                writer.WriteInt(this.bigInteger.Value);
                return;
            }

            writer.WriteInt(this.longValue);
        }

        private void SetLongValue(long value)
        {
            this.longValue = value;
            this.bigInteger = null;
            this.SetSize(value < int.MinValue || value > int.MaxValue ? IntegerSize.Long : IntegerSize.Int);
        }

        private void SetBigIntegerValue(BigInteger value)
        {
            if (value >= long.MinValue && value <= long.MaxValue)
            {
                this.SetLongValue((long)value);
            }
            else
            {
                this.longValue = 0;
                this.bigInteger = value;
                this.SetSize(IntegerSize.BigInteger);
            }
        }

        private void SetSize(IntegerSize size)
        {
            Debug.Assert(size != IntegerSize.Unknown, "size is Unknown");
            this.SetMetadata((int)size, IntSizeMask, IntSizeShift);
        }
    }
}
