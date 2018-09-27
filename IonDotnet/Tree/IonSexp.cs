namespace IonDotnet.Tree
{
    /// <inheritdoc />
    /// <summary>
    /// An Ion S-exp value.
    /// </summary>
    public sealed class IonSexp : IonSequence
    {
        public IonSexp() : this(false)
        {
        }

        private IonSexp(bool isNull) : base(isNull)
        {
        }

        /// <summary>
        /// Returns a new null.list value.
        /// </summary>
        public static IonSexp NewNull() => new IonSexp(true);

        public override IonType Type => IonType.Sexp;
    }
}
