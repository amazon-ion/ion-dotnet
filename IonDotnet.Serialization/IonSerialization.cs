namespace IonDotnet.Serialization
{
    public static class IonSerialization
    {
        public static readonly IonTextSerializer Text = new IonTextSerializer();
        
        public static readonly IonBinarySerializer Binary = new IonBinarySerializer();
    }
}
