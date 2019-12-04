using IonDotnet.Internals;

namespace IonDotnet.Tree.Impl
{
    /// <inheritdoc />
    /// <summary>
    /// Represent a null.null value.
    /// </summary>
    public sealed class IonNull : IonValue, IIonNull
    {
        public IonNull() : base(true)
        {
        }

        internal override void WriteBodyTo(IPrivateWriter writer) => writer.WriteNull();

        public override IonType Type => IonType.Null;
    }
}
