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
using System.Diagnostics;
using System.Numerics;
using Amazon.IonDotnet.Internals;

namespace Amazon.IonDotnet.Tree.Impl
{
    internal sealed class IonInt : IonValue, IIonInt
    {
        // This mask combine the 4th and 5th bit of the flag byte
        private const int IntSizeMask = 0x18;

        // Right shift 3 bits to get the value
        private const int IntSizeShift = 3;

        private long _longValue;
        private BigInteger? _bigInteger;

        public IonInt(long value) : base(false)
        {
            SetLongValue(value);
        }

        public IonInt(BigInteger value) : base(false)
        {
            SetBigIntegerValue(value);
        }

        private IonInt(bool isNull) : base(isNull)
        {
        }

        /// <summary>
        /// Returns a new null.int value.
        /// </summary>
        public static IonInt NewNull() => new IonInt(true);

        public override bool IsEquivalentTo(IIonValue other)
        {
            if (!base.IsEquivalentTo(other))
                return false;
            
            if (!(other is IonInt oInt))
                return false;
            if (NullFlagOn())
                return oInt.IsNull;
            if (oInt.IsNull)
                return false;
            if (oInt.IntegerSize != IntegerSize)
                return false;
            switch (IntegerSize)
            {
                case IntegerSize.Int:
                    return IntValue == oInt.IntValue;
                case IntegerSize.Long:
                    return LongValue == oInt.LongValue;
                case IntegerSize.BigInteger:
                    return BigIntegerValue == oInt.BigIntegerValue;
                default:
                    return false;
            }
        }

        internal override void WriteBodyTo(IPrivateWriter writer)
        {
            if (NullFlagOn())
            {
                writer.WriteNull(IonType.Int);
                return;
            }

            if (_bigInteger != null)
            {
                writer.WriteInt(_bigInteger.Value);
                return;
            }

            writer.WriteInt(_longValue);
        }

        public override int IntValue
        {
            get
            {
                ThrowIfNull();
                if (IntegerSize != IntegerSize.Int)
                    throw new OverflowException($"Size of this {nameof(IonInt)} is {IntegerSize}");
                return (int) _longValue;
            }
        }

        public override long LongValue
        {
            get
            {
                ThrowIfNull();
                if (_bigInteger != null)
                    throw new OverflowException($"Size of this {nameof(IonInt)} is {IntegerSize}");

                return _longValue;
            }
        }

        public override BigInteger BigIntegerValue
        {
            get
            {
                ThrowIfNull();
                return _bigInteger ?? new BigInteger(_longValue);
            }
        }

        public override IntegerSize IntegerSize
        {
            get
            {
                if (NullFlagOn())
                    return IntegerSize.Unknown;
                var metadata = GetMetadata(IntSizeMask, IntSizeShift);
                Debug.Assert(metadata < 3);
                return (IntegerSize) metadata;
            }
        }

        public override IonType Type() => IonType.Int;

        private void SetLongValue(long value)
        {
            _longValue = value;
            _bigInteger = null;
            SetSize(value < int.MinValue || value > int.MaxValue ? IntegerSize.Long : IntegerSize.Int);
        }

        private void SetBigIntegerValue(BigInteger value)
        {
            if (value >= long.MinValue && value <= long.MaxValue)
            {
                SetLongValue((long)value);
            }
            else
            {
                _longValue = 0;
                _bigInteger = value;
                SetSize(IntegerSize.BigInteger);
            }
        }

        private void SetSize(IntegerSize size)
        {
            Debug.Assert(size != IntegerSize.Unknown);
            SetMetadata((int)size, IntSizeMask, IntSizeShift);
        }
    }
}
