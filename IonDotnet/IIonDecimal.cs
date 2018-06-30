namespace IonDotnet
{
    public interface IIonDecimal : IIonValue<IIonDecimal>
    {
        decimal DecimalValue { get; set; }
    }
}
