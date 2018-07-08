namespace IonDotnet
{
    public interface IIonSymbol : IIonText, IIonValue<IIonSymbol>
    {
        SymbolToken SymbolValue { get; }
    }
}
