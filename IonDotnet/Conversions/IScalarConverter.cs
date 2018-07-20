using System;
using IonDotnet.Internals;

namespace IonDotnet.Conversions
{
    /// <summary>
    /// List of C# types that the ion value translates to
    /// </summary>
    [Flags]
    public enum ScalarType
    {
        Nothing = 0,
        Null = 1 << 0,
        Bool = 1 << 1,
        Int = 1 << 2,
        Long = 1 << 3,
        BigInteger = 1 << 4,
        Decimal = 1 << 5,
        Double = 1 << 6,
        String = 1 << 7,
        DateTime = 1 << 8
    }

    public interface IScalarConverter
    {
        void OnValueStart();

        void OnValueEnd();

        void OnSymbol(in SymbolToken symbolToken);

        T Convert<T>(in ValueVariant valueVariant);
    }
}
