namespace IonDotnet.Tree
{
    public interface IIonSymbol : IIonText
    {
        SymbolToken SymbolValue
        {
            get;
            set;
        }
    }
}
