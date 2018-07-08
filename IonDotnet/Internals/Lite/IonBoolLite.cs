namespace IonDotnet.Internals.Lite
{
    internal sealed class IonBoolLite : IonValueLite, IIonBool
    {
        public IonBoolLite(ContainerlessContext containerlessContext, bool isNull) : base(containerlessContext, isNull)
        {
        }

        public IonBoolLite(IonBoolLite existing, IContext context) : base(existing, context)
        {
        }

        public IIonBool Clone() => ClonePrivate(this, new ContainerlessContext(GetIonSystemLite()));


        public override IonType Type { get; } = IonType.Bool;

        public override void Accept(IValueVisitor visitor) => visitor.Visit(this);

        public bool BooleanValue
        {
            get => IsBoolTrue();
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
            throw new System.NotImplementedException();
        }

        protected override IonValueLite Clone(IContext parentContext) => ClonePrivate(this, parentContext);

        private static IonBoolLite ClonePrivate(IonBoolLite prototype, IContext parentContext) => new IonBoolLite(prototype, parentContext);
    }
}
