using System;

namespace IonDotnet.Tree
{
    public sealed class IonDatagram : IonSequence
    {
        public IonDatagram(bool isNull) : base(isNull)
        {
        }

        public override IonType Type => IonType.Datagram;

        public override IonValue Container
        {
            get => null;
            internal set => throw new InvalidOperationException("Cannot set the container of an Ion Datagram");
        }
    }
}
