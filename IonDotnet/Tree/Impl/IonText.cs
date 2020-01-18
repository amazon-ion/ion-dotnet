namespace IonDotnet.Tree.Impl
{
    /// <inheritdoc />
    /// <summary>
    /// Represent Ion textual values.
    /// </summary>
    internal abstract class IonText : IonValue, IIonText
    {
        protected string StringVal;

        protected IonText(string text, bool isNull) : base(isNull)
        {
            StringVal = text;
        }

        /// <summary>
        /// Textual value as string.
        /// </summary>
        public override string StringValue
        {
            get => StringVal;
        }

        public override void MakeNull()
        {
            base.MakeNull();
            StringVal = null;
        }
    }
}
