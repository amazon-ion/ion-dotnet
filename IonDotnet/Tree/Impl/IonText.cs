namespace IonDotnet.Tree.Impl
{
    /// <inheritdoc />
    /// <summary>
    /// Represent Ion textual values.
    /// </summary>
    public abstract class IonText : IonValue, IIonText
    {
        protected string StringVal;

        protected IonText(string text, bool isNull) : base(isNull)
        {
            StringVal = text;
        }

        /// <summary>
        /// Textual value as string.
        /// </summary>
        public virtual string StringValue
        {
            get => StringVal;
            set
            {
                ThrowIfLocked();
                NullFlagOn(value is null);
                StringVal = value;
            }
        }

        public override void MakeNull()
        {
            base.MakeNull();
            StringVal = null;
        }
    }
}
