namespace IonDotnet
{
    public interface IValueVisitor
    {
        void Visit(IIonValue value);
    }
}
