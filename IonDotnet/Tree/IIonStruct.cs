namespace IonDotnet.Tree
{
    public interface IIonStruct : IIonContainer
    {
        bool ContainsField(string fieldName);

        IIonValue GetField(string fieldName);

        void SetField(string fieldName, IIonValue value);

        bool RemoveField(string fieldName);
    }
}
