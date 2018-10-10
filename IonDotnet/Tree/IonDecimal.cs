using System;
using IonDotnet.Internals;

namespace IonDotnet.Tree
{
    public sealed class IonDecimal : IonValue
    {
        private BigDecimal _val;

        public IonDecimal(double doubleValue) : this(Convert.ToDecimal(doubleValue))
        {
        }

        public IonDecimal(decimal value) : this(new BigDecimal(value))
        {
        }

        public IonDecimal(BigDecimal bigDecimal) : base(false)
        {
            _val = bigDecimal;
        }

        private IonDecimal(bool isNull) : base(isNull)
        {
        }

        public static IonDecimal NewNull() => new IonDecimal(true);

        public override bool IsEquivalentTo(IonValue other)
        {
            if (!(other is IonDecimal otherDec))
                return false;
            if (NullFlagOn())
                return otherDec.IsNull;
            return !otherDec.IsNull && otherDec.DecimalValue == DecimalValue;
        }

        internal override void WriteBodyTo(IPrivateWriter writer)
        {
            if (NullFlagOn())
            {
                writer.WriteNull(IonType.Decimal);
                return;
            }

            writer.WriteDecimal(DecimalValue);
        }

        public decimal DecimalValue
        {
            get
            {
                ThrowIfNull();
                return _val.ToDecimal();
            }
            set
            {
                ThrowIfLocked();
                _val = new BigDecimal(value);
            }
        }

        public BigDecimal BigDecimalValue
        {
            get
            {
                ThrowIfNull();
                return _val;
            }
            set
            {
                ThrowIfLocked();
                _val = value;
            }
        }

        public override IonType Type => IonType.Decimal;
    }
}
