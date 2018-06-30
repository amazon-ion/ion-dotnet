namespace IonDotnet
{
    public interface IIonFloat : IIonValue<IIonFloat>
    {
        float FloatValue { get; set; }

        double DoubleValue { get; set; }

        /// <summary>
        /// whether this value is numeric. Returns true if this value is none of
        /// Null, NaN, +Inf or -Inf
        /// </summary>
        bool IsNumeric { get; }
    }
}
