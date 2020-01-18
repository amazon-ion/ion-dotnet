using System.Collections.Generic;

namespace IonDotnet.Tree
{
    public interface IIonContainer
    {
        int Count { get; }
        void Add(IIonValue item);
        void Clear();
        bool Contains(IIonValue item);
        void CopyTo(IIonValue[] array, int arrayIndex);
        IEnumerator<IIonValue> GetEnumerator();
        bool Remove(IIonValue item);
    }
}
