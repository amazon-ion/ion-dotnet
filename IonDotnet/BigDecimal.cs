using System;
using System.Numerics;

namespace IonDotnet
{
    public struct BigDecimal : IComparable<BigDecimal>, IEquatable<BigDecimal>
    {
        public static readonly BigDecimal NegativeZero = new BigDecimal(-1.0m * 0);
        public static readonly BigDecimal Zero = new BigDecimal(0, 0);
        public const int MaxPrecision = 100;

        internal readonly BigInteger IntVal;
        internal readonly int Scale;
        public readonly bool IsNegativeZero;

        public BigDecimal(BigInteger intVal, int scale)
        {
            if (Math.Abs(scale) > MaxPrecision)
            {
                throw new ArgumentException($"Maximum scale is {MaxPrecision}", nameof(scale));
            }

            IntVal = intVal;
            Scale = scale;
            IsNegativeZero = false;
        }

        public BigDecimal(decimal dec)
        {
            //TODO this is slow
            var integer = new BigInteger(dec);
            Scale = 0;
            var scaleFactor = 1m;
            while ((decimal) integer != dec * scaleFactor)
            {
                Scale += 1;
                scaleFactor *= 10;
                integer = (BigInteger) (dec * scaleFactor);
            }

            IntVal = integer;
            IsNegativeZero = dec == 0 && CheckNegativeZero(dec);
        }

        public int CompareTo(BigDecimal other)
        {
            var sdiff = Scale - other.Scale;
            if (sdiff == 0)
            {
                return BigInteger.Compare(IntVal, other.IntVal);
            }

            if (sdiff < 0)
            {
                var aValMultiplied = BigInteger.Multiply(IntVal, BigInteger.Pow(10, -sdiff));
                return aValMultiplied.CompareTo(other.IntVal);
            }

            //sdiff>0
            var bValMultiplied = BigInteger.Multiply(other.IntVal, BigInteger.Pow(10, sdiff));
            return BigInteger.Compare(IntVal, bValMultiplied);
        }

        public bool Equals(BigDecimal other) => CompareTo(other) == 0;

        public decimal ToDecimal()
        {
            //this will check for overflowing
            var dec = (decimal) IntVal;
            dec /= (decimal) Math.Pow(10, Scale);
            return dec;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is BigDecimal other && Equals(other);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        public static bool CheckNegativeZero(decimal dec)
        {
            if (dec != 0)
                return false;
            unsafe
            {
                var p = (byte*) &dec;
                return (p[3] & 0b_1000_0000) > 0;
            }
        }

        public static bool operator ==(BigDecimal a, BigDecimal b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(BigDecimal a, BigDecimal b)
        {
            return !(a == b);
        }

        public static BigDecimal operator +(BigDecimal a, BigDecimal b)
        {
            var rscale = a.Scale;
            var sdiff = rscale - b.Scale;
            var valA = a.IntVal;
            var valB = b.IntVal;
            if (sdiff < 0)
            {
                rscale = b.Scale;
                valA = BigInteger.Multiply(valA, BigInteger.Pow(10, -sdiff));
            }
            else if (sdiff > 0)
            {
                valB = BigInteger.Multiply(valB, BigInteger.Pow(10, sdiff));
            }

            return new BigDecimal(BigInteger.Add(valA, valB), rscale);
        }

        public static BigDecimal operator -(BigDecimal a, BigDecimal b)
        {
            // ReSharper disable once ArrangeRedundantParentheses
            return a + (-b);
        }

        public static BigDecimal operator *(BigDecimal a, BigDecimal b)
        {
            return new BigDecimal(BigInteger.Multiply(a.IntVal, b.IntVal), a.Scale + b.Scale);
        }

        public static BigDecimal operator /(BigDecimal a, BigDecimal b)
        {
            if (b.IntVal == 0)
            {
                throw new DivideByZeroException();
            }


            throw new NotImplementedException();
        }

        public static BigDecimal operator -(BigDecimal a)
        {
            return new BigDecimal(BigInteger.Negate(a.IntVal), a.Scale);
        }
    }
}
