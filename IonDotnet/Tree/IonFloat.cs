using System.Collections.Generic;
using IonDotnet.Internals;

namespace IonDotnet.Tree
{
    /// <inheritdoc />
    /// <summary>
    /// Ion value representing a floating point number.
    /// </summary>
    public sealed class IonFloat : IonValue
    {
        private double _d;

        public IonFloat(double value) : base(false)
        {
            _d = value;
        }

        private IonFloat(bool isNull) : base(isNull)
        {
        }

        /// <summary>
        /// Returns a new null.float value.
        /// </summary>
        public static IonFloat NewNull() => new IonFloat(true);

        public override bool IsEquivalentTo(IonValue other)
        {
            if (!(other is IonFloat oFloat))
                return false;

            if (NullFlagOn())
                return oFloat.IsNull;

            return !oFloat.IsNull && EqualityComparer<double>.Default.Equals(oFloat.Value, Value);
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
