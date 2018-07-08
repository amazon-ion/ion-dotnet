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
        protected IonException() : base()
        {
        }

        public IonException(Exception inner) : base("Exception caused by another", inner)
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
