using System;
using IonDotnet.Internals;

namespace IonDotnet.Tree
{
    public sealed class IonDecimal : IonValue
    {
        private decimal _decimal;

        public IonDecimal(decimal value) : base(false)
        {
            _decimal = value;
        }

        public IonDecimal(double doubleValue) : base(false)
        {
            _decimal = Convert.ToDecimal(doubleValue);
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
                return _decimal;
            }
            set
            {
                ThrowIfLocked();
                _decimal = value;
            }
        }

        public override IonType Type => IonType.Decimal;
    }
}
