namespace IonDotnet.Internals
{
    public interface IScalarConverter
    {
        string GetString(in ValueVariant valueVariant, ISymbolTable symbolTable);
    }
}
