namespace IonDotnet
{
    /// <summary>
    /// Common functionality of IonString and IonSymbol
    /// </summary>
    public interface IIonText<T> : IIonValue<T> where T : IIonValue
    {
        string StringValue { get; set; }
    }
}
