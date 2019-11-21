namespace IonDotnet.Systems
{
    public class UnexpectedEofException : IonException
    {
        public UnexpectedEofException()
        {
        }

        public UnexpectedEofException(long position) : base($"Unexpected EOF at position {position}")
        {
        }
    }
}
