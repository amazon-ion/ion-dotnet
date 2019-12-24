namespace IonDotnet.Tree
{
    public interface IIonSequence : IIonContainer
    {
        void RemoveAt(int index);
        int IndexOf(IIonValue item);
    }
}
