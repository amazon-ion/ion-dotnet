using IonDotnet.Internals;

namespace IonDotnet.Tree
{
    /// <summary>
    /// Ion object holding a boolean value
    /// </summary>
    public sealed class IonBool : IonValue
    {
        public IonBool(bool value) : base(false)
        {
            BoolTrueFlagOn(value);
        }

        public override bool Equals(IonValue other)
        {
            if (!(other is IonBool otherBool))
                return false;

            if (NullFlagOn())
                return otherBool.IsNull;

            return !otherBool.IsNull && otherBool.Value == Value;
        }

        internal override void WriteBodyTo(IPrivateWriter writer)
        {
            if (NullFlagOn())
            {
                writer.WriteNull(IonType.Bool);
            }
            else
            {
                writer.WriteBool(BoolTrueFlagOn());
            }
        }

        public override IonType Type => IonType.Bool;

        public bool Value
        {
            get
            {
                ThrowIfNull();
                return BoolTrueFlagOn();
            }
            set
            {
                ThrowIfLocked();
                NullFlagOn(false);
                BoolTrueFlagOn(value);
            }
        }

        /// <summary>
        /// Returns a new null.bool value.
        /// </summary>
        public static IonBool NewNull()
        {
            var v = new IonBool(false);
            v.MakeNull();
            return v;
        }
    }
}
