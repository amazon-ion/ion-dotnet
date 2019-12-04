namespace IonDotnet.Tree
{
    public interface IIonDecimal : IIonValue
    {
        decimal DecimalValue
        {
            get;
            set;
        }
        BigDecimal BigDecimalValue
        {
            get;
            set;
        }
    }
}
