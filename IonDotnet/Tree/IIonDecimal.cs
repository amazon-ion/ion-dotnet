namespace IonDotnet.Tree
{
    public interface IIonDecimal
    {
        decimal DecimalValue { get; set; }
        BigDecimal BigDecimalValue { get; set; }
    }
}
