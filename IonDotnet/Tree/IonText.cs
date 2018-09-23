namespace IonDotnet.Tree
{
    /// <summary>
    /// Represent Ion textual values.
    /// </summary>
    public abstract class IonText : IonValue
    {
        protected string _stringVal;

        protected IonText(string text) : base(text is null)
        {
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
                NullFlagOn(false);
                _stringVal = value;
            }
        }
    }
}
