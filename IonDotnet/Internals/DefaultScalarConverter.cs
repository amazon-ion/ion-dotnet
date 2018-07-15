using System;
using System.Numerics;
using static IonDotnet.Internals.ValueVariant;

namespace IonDotnet.Internals
{
    internal class DefaultScalarConverter : IScalarConverter
    {
        public BigInteger ToBigInteger(in ValueVariant valueVariant, ISymbolTable symbolTable)
        {
            if (valueVariant.TypeSet.HasFlag(ScalarType.BigInteger))
            {
                return valueVariant.BigIntegerValue;
            }

            // if (valueVariant.TypeSet.HasFlag(ValueVariant.ScalarType.Int))
            // {
            //     return new BigInteger(valueVariant.IntValue);
            // }

            // if (valueVariant.TypeSet.HasFlag(ValueVariant.ScalarType.Long))
            // {
            //     return new BigInteger(valueVariant.LongValue);
            // }

            throw new InvalidOperationException();
        }

        public bool ToBool(in ValueVariant valueVariant, ISymbolTable symbolTable)
        {
            if (!valueVariant.TypeSet.HasFlag(ScalarType.Bool)) throw new InvalidOperationException();

            return valueVariant.BoolValue;
        }

        public DateTime ToDateTime(in ValueVariant valueVariant, ISymbolTable symbolTable)
        {
            if (valueVariant.TypeSet.HasFlag(ScalarType.DateTime))
            {
                return valueVariant.DatetimeValue;
            }

            throw new InvalidOperationException();
        }

        public decimal ToDecimal(in ValueVariant valueVariant, ISymbolTable symbolTable)
        {
            if (valueVariant.TypeSet.HasFlag(ScalarType.Decimal))
            {
                return valueVariant.DecimalValue;
            }

            throw new InvalidOperationException();
        }

        public double ToDouble(in ValueVariant valueVariant, ISymbolTable symbolTable)
        {
            if (valueVariant.TypeSet.HasFlag(ScalarType.Double))
            {
                return valueVariant.DoubleValue;
            }

            throw new InvalidOperationException();
        }

        public int ToInt(in ValueVariant valueVariant, ISymbolTable symbolTable)
        {
            if (valueVariant.TypeSet.HasFlag(ScalarType.Int))
            {
                return valueVariant.IntValue;
            }

            throw new InvalidOperationException();
        }

        public long ToLong(in ValueVariant valueVariant, ISymbolTable symbolTable)
        {
            if (valueVariant.TypeSet.HasFlag(ScalarType.Long))
            {
                return valueVariant.LongValue;
            }

            throw new InvalidOperationException();
        }

        public string ToString(in ValueVariant valueVariant, ISymbolTable symbolTable)
        {
            if (!valueVariant.TypeSet.HasFlag(ScalarType.String)) throw new IonException("no string value");
            return valueVariant.StringValue;
        }
    }
}