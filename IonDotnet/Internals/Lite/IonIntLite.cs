using System.Diagnostics;
using System.Numerics;

namespace IonDotnet.Internals.Lite
{
    internal sealed class IonIntLite : IonValueLite, IIonInt
    {
        private static readonly int HashSignature = IonType.Int.ToString().GetHashCode();

        // This mask combine the 4th and 5th bit of the flag byte
        private const int IntSizeMask = 0x18;

        // Right shift 3 bits to get the value
        private const int IntSizeShift = 3;

        private long _longValue;
        private BigInteger? _bigInteger;

        public IonIntLite(ContainerlessContext containerlessContext, bool isNull) : base(containerlessContext, isNull)
        {
        }

        public IonIntLite(IonIntLite existing, IContext context) : base(existing, context)
        {
        }

        protected override int GetHashCode(ISymbolTableProvider symbolTableProvider)
        {
            var result = HashSignature;

            if (IsNullValue()) return HashTypeAnnotations(result, symbolTableProvider);

            if (_bigInteger == null)
            {
                var lv = LongValue;
                // Throw away top 32 bits if they're not interesting.
                // Otherwise n and -(n+1) get the same hash code.
                result ^= (int) lv;
                var hiWord = (int) (lv >> 32);
                if (hiWord != 0 && hiWord != -1)
                {
                    result ^= hiWord;
                }
            }
            else
            {
                result = _bigInteger.Value.GetHashCode();
            }

            return HashTypeAnnotations(result, symbolTableProvider);
        }

        public override IonValueLite Clone(IContext parentContext) => ClonePrivate(this, parentContext);

        protected override void WriteBodyTo(IIonWriter writer, ISymbolTableProvider symbolTableProvider)
        {
            if (IsNullValue())
            {
                writer.WriteNull(IonType.Int);
            }
            else if (_bigInteger != null)
            {
                writer.WriteInt(_bigInteger.Value);
            }
            else
            {
                writer.WriteInt(_longValue);
            }
        }

        public IIonInt Clone() => ClonePrivate(this, new ContainerlessContext(GetIonSystemLite()));

        public override IonType Type => IonType.Int;

        public override void Accept(IValueVisitor visitor) => visitor.Visit(this);

        public int IntValue
        {
            get
            {
                ThrowIfNull();
                return _bigInteger == null ? (int) _longValue : (int) _bigInteger;
            }
            set => LongValue = value;
        }

        public long LongValue
        {
            get
            {
                ThrowIfNull();
                return _bigInteger == null ? _longValue : (long) _bigInteger;
            }
            set
            {
                CheckLocked();
                IsNullValue(false);
                _longValue = value;
                _bigInteger = null;
                SetSize(value < int.MinValue || value > int.MaxValue
                    ? IntegerSize.Int
                    : IntegerSize.Long);
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
                CheckLocked();
                IsNullValue(false);
                if (value >= long.MinValue && value <= long.MaxValue)
                {
                    LongValue = (long) value;
                    return;
                }

                _longValue = 0;
                _bigInteger = value;
                SetSize(IntegerSize.BigInteger);
            }
        }

        public void SetNull()
        {
            CheckLocked();
            _bigInteger = null;
            _longValue = 0;
            IsNullValue(true);
        }

        public IntegerSize Size
        {
            get
            {
                if (IsNullValue()) return IntegerSize.Unknown;
                var metadata = GetMetadata(IntSizeMask, IntSizeShift);
                Debug.Assert(metadata < 3);
                return (IntegerSize) metadata;
            }
        }

        private void SetSize(IntegerSize size)
        {
            Debug.Assert(size != IntegerSize.Unknown);
            SetMetadata((uint) size, IntSizeMask, IntSizeShift);
        }

        private static IonIntLite ClonePrivate(IonIntLite prototype, IContext parentContext) => new IonIntLite(prototype, parentContext);
    }
}
