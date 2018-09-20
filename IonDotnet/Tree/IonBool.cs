using IonDotnet.Internals;

namespace IonDotnet.Tree
{
    /// <summary>
    /// Ion object holding a boolean value
    /// </summary>
    public sealed class IonBool : IonValue
    {
        public IonBool(bool value, bool isNull = false) : base(isNull)
        {
            BoolTrueFlagOn(value);
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
                //this will do the read-only check
                IsNull = false;
                BoolTrueFlagOn(value);
            }
        }
    }
}
