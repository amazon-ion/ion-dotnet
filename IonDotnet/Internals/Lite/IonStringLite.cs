namespace IonDotnet.Internals.Lite
{
    internal sealed class IonStringLite : IonTextLite, IIonString
    {
        private static readonly int HashSignature = IonType.String.ToString().GetHashCode();

        public IonStringLite(ContainerlessContext containerlessContext, bool isNull) : base(containerlessContext, isNull)
        {
        }

        public IonStringLite(IonStringLite existing, IContext context) : base(existing, context)
        {
        }

        protected override int GetHashCode(ISymbolTableProvider symbolTableProvider)
        {
            var result = HashSignature;

            if (!IsNullValue())
            {
                result ^= TextValue.GetHashCode();
            }

            return HashTypeAnnotations(result, symbolTableProvider);
        }

        public override IonValueLite Clone(IContext parentContext) => ClonePrivate(this, parentContext);

        protected override void WriteBodyTo(IIonWriter writer, ISymbolTableProvider symbolTableProvider) => writer.WriteString(TextValue);

        public IIonString Clone() => ClonePrivate(this, new ContainerlessContext(GetIonSystemLite()));

        public override IonType Type => IonType.String;

        public override void Accept(IValueVisitor visitor) => visitor.Visit(this);

        private static IonStringLite ClonePrivate(IonStringLite prototype, IContext context) => new IonStringLite(prototype, context);
    }
}
