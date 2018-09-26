using IonDotnet.Internals;

namespace IonDotnet.Tree
{
    /// <inheritdoc />
    /// <summary>
    /// An Ion string value.
    /// </summary>
    public sealed class IonString : IonText
    {
        public IonString(string value) : base(value, value is null)
        {
        }

        public override bool Equals(IonValue other)
        {
            if (!(other is IonString otherString))
                return false;
            return _stringVal == otherString._stringVal;
        }

        internal override void WriteBodyTo(IPrivateWriter writer) => writer.WriteString(_stringVal);

        public override IonType Type => IonType.String;
    }
}
