using IonDotnet.Internals;

namespace IonDotnet.Tree.Impl
{
    /// <inheritdoc />
    /// <summary>
    /// An Ion string value.
    /// </summary>
    internal sealed class IonString : IonText, IIonString
    {
        public IonString(string value) : base(value, value is null)
        {
        }

        /// <summary>
        /// Returns a new null.string value.
        /// </summary>
        public static IonString NewNull() => new IonString(null);

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
