using IonDotnet.Internals;

namespace IonDotnet.Tree
{
    public sealed class IonTimestamp : IonValue
    {
        private Timestamp _timestamp;

        public IonTimestamp(Timestamp val, bool isNull) : base(isNull)
        {
            _timestamp = val;
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
