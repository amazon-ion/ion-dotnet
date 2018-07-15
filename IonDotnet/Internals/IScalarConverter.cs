using System;
using System.Numerics;

namespace IonDotnet.Internals
{
    public interface IScalarConverter
    {
        string ToString(in ValueVariant valueVariant, ISymbolTable symbolTable);
        long ToLong(in ValueVariant valueVariant, ISymbolTable symbolTable);
        bool ToBool(in ValueVariant valueVariant, ISymbolTable symbolTable);
        decimal ToDecimal(in ValueVariant valueVariant, ISymbolTable symbolTable);
        double ToDouble(in ValueVariant valueVariant, ISymbolTable symbolTable);
        int ToInt(in ValueVariant valueVariant, ISymbolTable symbolTable);
        DateTime ToDateTime(in ValueVariant valueVariant, ISymbolTable symbolTable);
        BigInteger ToBigInteger(in ValueVariant valueVariant, ISymbolTable symbolTable);
    }
}
