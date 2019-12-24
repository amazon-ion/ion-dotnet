namespace IonDotnet.Tree
{
    public interface IIonDatagram : IIonSequence
    {
        void WriteTo(IIonWriter writer);
    }
}
