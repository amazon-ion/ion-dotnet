namespace IonDotnet.Tree
{
    public interface IIonTimestamp : IIonValue
    {
        Timestamp Value
        {
            get;
            set;
        }
    }
}
