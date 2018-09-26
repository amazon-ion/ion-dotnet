namespace IonDotnet.Tree
{
    /// <inheritdoc />
    /// <summary>
    /// Represent Ion textual values.
    /// </summary>
    public abstract class IonText : IonValue
    {
        protected string _stringVal;

        protected IonText(string text, bool isNull) : base(isNull)
        {
            _stringVal = text;
        }

        /// <summary>
        /// Textual value as string.
        /// </summary>
        public virtual string StringValue
        {
            get => _stringVal;
            set
            {
                ThrowIfLocked();
                NullFlagOn(value is null);
                _stringVal = value;
            }
        }

        public override void MakeNull()
        {
            base.MakeNull();
            _stringVal = null;
        }
    }
}
