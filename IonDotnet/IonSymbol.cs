namespace IonDotnet
{
    public interface IIonSymbol : IIonText<IIonSymbol>
    {
        SymbolToken SymbolValue { get; }
    }
}
