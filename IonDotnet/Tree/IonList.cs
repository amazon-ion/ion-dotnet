namespace IonDotnet.Tree
{
    public sealed class IonList : IonSequence
    {
        public IonList() : this(false)
        {
        }

        private IonList(bool isNull) : base(isNull)
        {
        }

        /// <summary>
        /// Returns a new null.list value.
        /// </summary>
        public static IonList NewNull() => new IonList(true);

        public override bool Equals(IonValue other)
        {
            throw new System.NotImplementedException();
        }

        public override IonType Type => IonType.List;
    }
}
