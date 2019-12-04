using IonDotnet.Tree.Impl;

namespace IonDotnet
{
    public class ContainedValueException : IonException
    {
        public ContainedValueException() : this(string.Empty)
        {
        }

        public ContainedValueException(IonValue value) : this(value.ToString())
        {
        }

        private ContainedValueException(string valueString) : base($"Value {valueString} is already child of a container")
        {
        }
    }
}
