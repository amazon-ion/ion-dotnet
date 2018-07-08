namespace IonDotnet.Internals.Lite
{
    internal sealed class IonBoolLite : IonValueLite, IIonBool
    {
        private static readonly int HashSignature = IonType.Bool.ToString().GetHashCode();
        private static readonly int TrueHash = HashSignature ^ (unchecked(16777619 * 1231));
        private static readonly int FalseHash = HashSignature ^ (unchecked(16777619 * 1237));

        public IonBoolLite(ContainerlessContext containerlessContext, bool isNull) : base(containerlessContext, isNull)
        {
        }

        public IonBoolLite(IonBoolLite existing, IContext context) : base(existing, context)
        {
        }

        public IIonBool Clone() => ClonePrivate(this, new ContainerlessContext(GetIonSystemLite()));

        public override IonType Type => IonType.Bool;

        public override void Accept(IValueVisitor visitor) => visitor.Visit(this);

        public bool BooleanValue
        {
            get
            {
                ThrowIfNull();
                return IsBoolTrue();
            }
            set
            {
                CheckLocked();
                IsBoolTrue(value);
                IsNullValue(false);
            }
        }

        public void SetValue(bool? val)
        {
            CheckLocked();
            if (val == null)
            {
                IsNullValue(true);
                return;
            }

            BooleanValue = val.Value;
        }


        protected override int GetHashCode(ISymbolTableProvider symbolTableProvider)
        {
            if (!IsNull) return BooleanValue ? TrueHash : FalseHash;
            return HashTypeAnnotations(HashSignature, symbolTableProvider);
        }

        public override IonValueLite Clone(IContext parentContext) => ClonePrivate(this, parentContext);

        protected override void WriteBodyTo(IIonWriter writer, ISymbolTableProvider symbolTableProvider)
        {
            if (IsNullValue())
            {
                writer.WriteNull(IonType.Bool);
                return;
            }

            writer.WriteBool(IsBoolTrue());
        }

        private static IonBoolLite ClonePrivate(IonBoolLite prototype, IContext parentContext) => new IonBoolLite(prototype, parentContext);
    }
}
