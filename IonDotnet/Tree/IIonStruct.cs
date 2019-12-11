namespace IonDotnet.Tree
{
    public interface IIonStruct : IIonContainer
    {
        void Clear();
        bool RemoveField(string fieldName);
        bool ContainsField(string fieldName);
    }
}
