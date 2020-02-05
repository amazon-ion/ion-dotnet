namespace Amazon.IonDotnet
{
    public class UnknownSymbolException : IonException
    {
        public readonly int Sid;

        public UnknownSymbolException(int sid) : base($"Unknown text for sid {sid}")
        {
            Sid = sid;
        }
    }
}
