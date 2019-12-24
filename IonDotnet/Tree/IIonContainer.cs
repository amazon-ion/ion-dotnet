using System.Collections.Generic;

namespace IonDotnet.Tree
{
    public interface IIonContainer
    {
        int Count { get; }
        void Clear();
        void Add(IIonValue item);
        bool Remove(IIonValue item);
        bool Contains(IIonValue item);
        void CopyTo(IIonValue[] array, int arrayIndex);
        IEnumerator<IIonValue> GetEnumerator();
        IIonContainer Container { get; set; }
    }
}
