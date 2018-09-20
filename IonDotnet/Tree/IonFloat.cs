using IonDotnet.Internals;

namespace IonDotnet.Tree
{
    /// <summary>
    /// Ion value representing a floating point number.
    /// </summary>
    public sealed class IonFloat : IonValue
    {
        private double _d;

        public IonFloat(bool isNull) : base(isNull)
        {
        }

        internal override void WriteBodyTo(IPrivateWriter writer)
        {
            if (NullFlagOn())
            {
                writer.WriteNull(IonType.Float);
                return;
            }

            writer.WriteFloat(_d);
        }

        /// <summary>
        /// Get or set the value of this float as <see cref="System.Double"/>.
        /// </summary>
        public double Value
        {
            get
            {
                ThrowIfNull();
                return _d;
            }
            set
            {
                ThrowIfLocked();
                NullFlagOn(false);
                _d = value;
            }
        }

        public override IonType Type => IonType.Float;
    }
}
