namespace IonDotnet.Internals.Text
{
    public class InvalidTokenException : IonException
    {
        public InvalidTokenException()
        {
        }

        public InvalidTokenException(string message) : base(message)
        {
        }

        public InvalidTokenException(int token) : base($"Token {(char) token} is not expected")
        {
        }
    }
}
