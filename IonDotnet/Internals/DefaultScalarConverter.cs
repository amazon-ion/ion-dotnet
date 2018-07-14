namespace IonDotnet.Internals
{
    internal class DefaultScalarConverter : IScalarConverter
    {
        public string GetString(in ValueVariant valueVariant, ISymbolTable symbolTable)
        {
            if (!valueVariant.HasString) throw new IonException("no string value");
            return valueVariant.StringValue;
        }
    }
}
