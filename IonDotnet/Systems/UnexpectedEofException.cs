namespace IonDotnet.Systems
{
    public class UnexpectedEofException : IonException
    {
        public UnexpectedEofException():base()
        {
        }
        
        public UnexpectedEofException(long position) : base($"Unexpected EOF at position {position}")
        {
        }
    }
}