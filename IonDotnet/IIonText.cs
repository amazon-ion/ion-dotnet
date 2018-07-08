namespace IonDotnet
{
    /// <summary>
    /// Common functionality of IonString and IonSymbol
    /// </summary>
    public interface IIonText : IIonValue
    {
        string StringValue { get; set; }
    }
}
