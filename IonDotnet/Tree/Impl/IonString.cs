using IonDotnet.Internals;

namespace IonDotnet.Tree.Impl
{
    /// <inheritdoc />
    /// <summary>
    /// An Ion string value.
    /// </summary>
    public sealed class IonString : IonText, IIonString
    {
        public IonString(string value) : base(value, value is null)
        {
        }

        public override bool IsEquivalentTo(IonValue other)
        {
            if (!base.IsEquivalentTo(other))
                return false;
            
            if (!(other is IonString otherString))
                return false;
            return StringVal == otherString.StringVal;
        }

        internal override void WriteBodyTo(IPrivateWriter writer) => writer.WriteString(StringVal);

        public override IonType Type => IonType.String;
    }
}
