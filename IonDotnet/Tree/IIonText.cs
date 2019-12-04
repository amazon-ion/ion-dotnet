namespace IonDotnet.Tree
{
    public interface IIonText : IIonValue
    {
        string StringValue
        {
            get;
            set;
        }
        void MakeNull();
    }
}
