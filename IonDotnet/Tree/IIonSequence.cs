using IonDotnet.Tree.Impl;
namespace IonDotnet.Tree
{
    public interface IIonSequence : IIonContainer
    {
        int IndexOf(IonValue item);
        void Add(IonValue item);
        void Insert(int index, IonValue item);
        void RemoveAt(int index);
        void Clear();
        bool Contains(IonValue item);
    }
}
