using IonDotnet.Internals;

namespace IonDotnet.Tree
{
    /// <summary>
    /// Represent a null.null value.
    /// </summary>
    public sealed class IonNull : IonValue
    {
        public IonNull() : base(true)
        {
        }

        public override bool Equals(IonValue other) => other is IonNull;

        internal override void WriteBodyTo(IPrivateWriter writer) => writer.WriteNull();

        public override IonType Type => IonType.Null;
    }
}
