using System;
using System.Collections.Generic;
using IonDotnet.Internals;
using IonDotnet.Utils;

namespace IonDotnet.Tree.Impl
{
    /// <inheritdoc />
    /// <summary>
    /// Ion value representing a floating point number.
    /// </summary>
    public sealed class IonFloat : IonValue, IIonFloat
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
            if (!base.IsEquivalentTo(other))
                return false;

            var oFloat = (IonFloat)other;

            if (NullFlagOn())
                return oFloat.IsNull;
            if (oFloat.IsNull)
                return false;

            if (PrivateHelper.IsNegativeZero(_d) ^ PrivateHelper.IsNegativeZero(oFloat._d))
                return false;

            return EqualityComparer<double>.Default.Equals(oFloat.Value, Value);
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
