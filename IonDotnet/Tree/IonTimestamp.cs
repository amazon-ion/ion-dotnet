using IonDotnet.Internals;

namespace IonDotnet.Tree
{
    public sealed class IonTimestamp : IonValue
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

        public override bool Equals(IonValue other)
        {
            throw new System.NotImplementedException();
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
