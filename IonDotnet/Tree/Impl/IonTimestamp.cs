using IonDotnet.Internals;

namespace IonDotnet.Tree.Impl
{
    public sealed class IonTimestamp : IonValue, IIonTimestamp
    {
        private Timestamp _timestamp;

        public IonTimestamp(Timestamp val) : base(false)
        {
            _timestamp = val;
        }

        private IonTimestamp(bool isNull) : base(isNull)
        {
        }

        /// <summary>
        /// Returns a new null.timestamp value.
        /// </summary>
        public static IonTimestamp NewNull() => new IonTimestamp(true);

        public override bool IsEquivalentTo(IonValue other)
        {
            if (!base.IsEquivalentTo(other))
                return false;
            
            if (!(other is IonTimestamp oTimestamp))
                return false;
            if (NullFlagOn())
                return other.IsNull;

            return !other.IsNull && _timestamp == oTimestamp._timestamp;
        }

        internal override void WriteBodyTo(IPrivateWriter writer)
        {
            if (NullFlagOn())
            {
                writer.WriteNull(IonType.Timestamp);
                return;
            }

            writer.WriteTimestamp(_timestamp);
        }

        public Timestamp Value
        {
            get
            {
                ThrowIfNull();
                return _timestamp;
            }
            set
            {
                ThrowIfLocked();
                _timestamp = value;
            }
        }

        public override IonType Type => IonType.Timestamp;
    }
}
