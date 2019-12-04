using System.Collections.Generic;
using IonDotnet.Tree.Impl;

namespace IonDotnet.Tree
{
    public interface IIonContainer : IIonValue
    {
        void MakeNull();
        int Count { get; }
        bool Remove(IonValue item);
        IEnumerator<IonValue> GetEnumerator();
    }
}
