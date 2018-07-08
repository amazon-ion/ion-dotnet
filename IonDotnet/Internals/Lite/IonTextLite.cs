namespace IonDotnet.Internals.Lite
{
    internal abstract class IonTextLite : IonValueLite, IIonText
    {
        protected string TextValue;

        protected IonTextLite(ContainerlessContext containerlessContext, bool isNull) : base(containerlessContext, isNull)
        {
        }

        protected IonTextLite(IonTextLite existing, IContext context) : base(existing, context)
        {
            TextValue = existing.TextValue;
        }

        public string StringValue
        {
            get => TextValue;
            set
            {
                CheckLocked();
                TextValue = value;
                IsNullValue(value == null);
            }
        }
    }
}
