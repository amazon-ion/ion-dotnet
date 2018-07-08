namespace IonDotnet
{
    public interface IIonBlob : IIonLob, IIonValue<IIonBlob>
    {
        void PrintBase64();
    }
}
