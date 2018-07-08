namespace IonDotnet.Internals.Lite
{
    internal abstract class IonSequenceLite : IonContainerLite, IIonSequence
    {
        protected IonSequenceLite(ContainerlessContext containerlessContext, bool isNull) : base(containerlessContext, isNull)
        {
        }

        protected IonSequenceLite(IonContainerLite existing, IContext context, bool isStruct) : base(existing, context, isStruct)
        {
        }

        public abstract int IndexOf(IIonValue item);
        public abstract void Insert(int index, IIonValue item);
        public abstract void RemoveAt(int index);
        public abstract IIonValue this[int index] { get; set; }
        public abstract IValueFactory Add();
    }
}
