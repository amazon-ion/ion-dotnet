namespace IonDotnet.Tree
{
    /// <summary>
    /// Represent Ion textual values.
    /// </summary>
    public abstract class IonText : IonValue
    {
        protected string _stringVal;

        protected IonText(bool isNull) : base(isNull)
        {
            if (isNull)
            {
                _stringVal = null;
            }
        }

        /// <summary>
        /// Textual value as string.
        /// </summary>
        public string StringValue
        {
            get => _stringVal;
            set
            {
                ThrowIfLocked();
                NullFlagOn(false);
                _stringVal = value;
            }
        }
    }
}
