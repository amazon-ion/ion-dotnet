using System;
using System.Text;

namespace IonDotnet
{
    /// <inheritdoc />
    /// <summary>
    /// Base class for exceptions thrown throughout this library
    /// </summary>
    public class IonException : Exception
    {
        public IonException() : base()
        {
        }

        public IonException(string message) : base(message)
        {
            
        }

        public IonException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
