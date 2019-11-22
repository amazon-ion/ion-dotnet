namespace IonDotnet.Internals.Text
{
    public class InvalidTokenException : IonException
    {
        public InvalidTokenException(string message) : base(message)
        {
        }

        public InvalidTokenException(int token) : base($"Token {(char) token} : {token} is not expected")
        {
        }
    }
}
