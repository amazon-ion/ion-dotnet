namespace IonDotnet.Internals.Lite
{
    internal class IonNullLite : IonValueLite, IIonNull
    {
        private const int HashSignature = -1535290537;

        public IonNullLite(ContainerlessContext containerlessContext) : base(containerlessContext, true)
        {
        }

        private IonNullLite(IonNullLite existing, IContext context) : base(existing, context)
        {
        }

        protected override int GetHashCode(ISymbolTableProvider symbolTableProvider)
            => HashTypeAnnotations(HashSignature, symbolTableProvider);

        public override IonValueLite Clone(IContext parentContext)
            => ClonePrivate(this, parentContext);

        protected override void WriteBodyTo(IIonWriter writer, ISymbolTableProvider symbolTableProvider) => writer.WriteNull();

        public IIonNull Clone() => ClonePrivate(this, new ContainerlessContext(GetIonSystemLite()));

        public override IonType Type => IonType.Null;

        public override void Accept(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        private static IonNullLite ClonePrivate(IonNullLite prototype, IContext parentContext) => new IonNullLite(prototype, parentContext);
    }
}
