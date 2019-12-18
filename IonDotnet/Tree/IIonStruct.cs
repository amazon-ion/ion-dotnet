namespace IonDotnet.Tree
{
    public interface IIonStruct : IIonContainer
    {
        bool RemoveField(string fieldName);
        bool ContainsField(string fieldName);
    }
}
