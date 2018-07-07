using System.Collections;
using System.Collections.Generic;

namespace IonDotnet.Internals.Lite
{
    internal abstract class IonContainerLite : IonValueLite, IPrivateIonContainer, IContext
    {
        public abstract IEnumerator<IIonValue> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        protected IonContainerLite(ContainerlessContext containerlessContext, bool isNull) : base(containerlessContext, isNull)
        {
        }

        protected IonContainerLite(IonValueLite existing, IContext context) : base(existing, context)
        {
        }

        public abstract void MakeNull();
        public abstract bool IsEmpty { get; }
        public abstract int GetChildCount();
        public abstract IIonValue GetChild(int index);
        public abstract IonContainerLite GetContextContainer();
        public abstract IonSystemLite GetSystem();
        public abstract ISymbolTable GetContextSymbolTable();
    }
}
