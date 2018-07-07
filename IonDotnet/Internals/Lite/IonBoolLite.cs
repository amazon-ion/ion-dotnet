namespace IonDotnet.Internals.Lite
{
    internal sealed class IonBoolLite : IonValueLite, IIonBool
    {
        public IonBoolLite(ContainerlessContext containerlessContext, bool isNull) : base(containerlessContext, isNull)
        {
        }

        public IonBoolLite(IonValueLite existing, IContext context) : base(existing, context)
        {
        }

        public IIonBool Clone() => new IonBoolLite(this, new ContainerlessContext(GetIonSystemLite()));

        protected override int GetHashCode(ISymbolTableProvider symbolTableProvider)
        {
            throw new System.NotImplementedException();
        }

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
    }
}
