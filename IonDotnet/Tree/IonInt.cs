using System;
using System.Diagnostics;
using System.Numerics;
using IonDotnet.Internals;

namespace IonDotnet.Tree
{
    public sealed class IonInt : IonValue
    {
        // This mask combine the 4th and 5th bit of the flag byte
        private const int IntSizeMask = 0x18;

        // Right shift 3 bits to get the value
        private const int IntSizeShift = 3;

        private long _longValue;
        private BigInteger? _bigInteger;

        public IonInt(long value) : base(false)
        {
            _longValue = value;
        }
        
        public IonInt(BigInteger value) : base(false)
        {
            _bigInteger = value;
        }

        private IonInt(bool isNull) : base(isNull)
        {
        }

        /// <summary>
        /// Returns a new null.int value.
        /// </summary>
        public static IonInt NewNull() => new IonInt(true);

        public override bool Equals(IonValue other)
        {
            throw new NotImplementedException();
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

        public int IntValue
        {
            get
            {
                ThrowIfNull();
                if (IntSize != IntegerSize.Int)
                    throw new OverflowException($"Size of this {nameof(IonInt)} is {IntSize}");
                return (int) _longValue;
            }
            set => LongValue = value;
        }

        public long LongValue
        {
            get
            {
                ThrowIfNull();
                if (_bigInteger != null)
                    throw new OverflowException($"Size of this {nameof(IonInt)} is {IntSize}");

                return _longValue;
            }
            set
            {
                ThrowIfLocked();
                NullFlagOn(false);
                _longValue = value;
                _bigInteger = null;
                SetSize(value < int.MinValue || value > int.MaxValue ? IntegerSize.Long : IntegerSize.Int);
            }
        }

        public BigInteger BigIntegerValue
        {
            get
            {
                ThrowIfNull();
                return _bigInteger ?? new BigInteger(_longValue);
            }
            set
            {
                ThrowIfLocked();
                NullFlagOn(false);
                if (value >= long.MinValue || value <= long.MaxValue)
                {
                    LongValue = (long) value;
                    return;
                }

                _longValue = 0;
                _bigInteger = value;
                SetSize(IntegerSize.BigInteger);
            }
        }

        public IntegerSize IntSize
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

        public override IonType Type => IonType.Int;

        private void SetSize(IntegerSize size)
        {
            Debug.Assert(size != IntegerSize.Unknown);
            SetMetadata((int) size, IntSizeMask, IntSizeShift);
        }
    }
}
