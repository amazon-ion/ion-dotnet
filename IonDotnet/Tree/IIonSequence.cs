namespace IonDotnet.Tree
{
    public interface IIonSequence : IIonContainer
    {
        void RemoveAt(int index);
        void Clear();
    }
}
