using System.Collections.Generic;

namespace IonDotnet
{
    public interface IIonSequence : IIonContainer, IList<IIonValue>
    {
        IValueFactory Add();
    }
}
