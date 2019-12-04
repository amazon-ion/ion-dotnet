namespace IonDotnet.Tree.Impl
{
    public interface IIonStruct : IIonContainer
    {
        void Add(IonValue item);
        void Add(string fieldName, IonValue value);
        void Clear();
        bool Contains(IonValue item);
        bool RemoveField(string fieldName);
        bool ContainsField(string fieldName);
    }
}
