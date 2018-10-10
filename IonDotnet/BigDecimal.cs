using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using IonDotnet.Utils;

// ReSharper disable ImpureMethodCallOnReadonlyValueField

namespace IonDotnet
{
    /// <inheritdoc cref="IComparable" />
    /// <summary>
    /// Represent a decimal-type number. This type extends <see cref="T:System.Decimal" /> to allow for larger number range and 
    /// decimal places up to <see cref="F:IonDotnet.BigDecimal.MaxPrecision" />.
    /// </summary>
    public readonly struct BigDecimal : IComparable<BigDecimal>, IEquatable<BigDecimal>
    {
        public const int MaxPrecision = 1000;

        internal readonly BigInteger IntVal;
        internal readonly int Scale;
        public readonly bool IsNegativeZero;

        public BigDecimal(BigInteger intVal, int scale, bool negate = false)
        {
            if (scale > MaxPrecision)
            {
                throw new ArgumentException($"Maximum scale is {MaxPrecision}", nameof(scale));
            }

            IntVal = negate ? BigInteger.Negate(intVal) : intVal;
            Scale = scale;
            IsNegativeZero = negate && intVal == 0;
        }

        public BigDecimal(decimal dec)
        {
            Span<byte> dBytes = stackalloc byte[sizeof(decimal)];
            var maxId = DecimalHelper.CopyDecimalBigEndian(dBytes, dec);
            Scale = dBytes[2];

            var bi = BigInteger.Zero;
            for (var idx = 4; idx <= maxId; idx++)
            {
                bi <<= 8;
                bi += dBytes[idx];
            }

            var negative = (dBytes[3] & 0x80) > 0;
            IntVal = negative ? BigInteger.Negate(bi) : bi;
            IsNegativeZero = negative && IntVal == 0;
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

        private static readonly BigInteger DecimalMaxValue = new BigInteger(decimal.MaxValue);

        public decimal ToDecimal()
        {
            var intVal = IntVal;
            var scale = Scale;
            while (BigInteger.Abs(intVal) > DecimalMaxValue && scale > 0)
            {
                intVal /= 10;
                scale--;
            }

            //this will check for overflowing if |intVal|>DecimalMaxValue
            var dec = (decimal) intVal;
            while (scale > 0)
            {
                var step = scale > 28 ? 28 : scale;
                dec /= (decimal) Math.Pow(10, step);
                scale -= step;
            }

            return dec;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is BigDecimal other && Equals(other);
        }

        public override int GetHashCode() => (int) ((Scale + 31 * (long) IntVal.GetHashCode()) % 2147483647);

        public static bool CheckNegativeZero(decimal dec)
        {
            if (dec != 0)
            {
                return false;
            }

            unsafe
            {
                var p = (byte*) &dec;
                return (p[3] & 0b_1000_0000) > 0;
            }
        }

        public static bool operator >(BigDecimal a, BigDecimal b)
        {
            return a.CompareTo(b) > 0;
        }

        public static bool operator <(BigDecimal a, BigDecimal b)
        {
            return a.CompareTo(b) < 0;
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

            var preferredScale = Math.Max(a.Scale, b.Scale);
            var aMag = a.IntVal;
            var bMag = b.IntVal;
            //round a and b to the same preferredScale
            if (a.Scale < preferredScale)
            {
                aMag = aMag * BigInteger.Pow(10, preferredScale - a.Scale);
            }

            if (b.Scale < preferredScale)
            {
                bMag = bMag * BigInteger.Pow(10, preferredScale - b.Scale);
            }

            var outIntVal = BigInteger.DivRem(aMag, bMag, out var remainder);
            var scale = 0;
            while (remainder != 0 && scale < preferredScale)
            {
                //increase the output scale
                while (BigInteger.Abs(remainder) < BigInteger.Abs(bMag))
                {
                    remainder *= 10;
                    scale++;
                    outIntVal *= 10;
                }

                outIntVal = outIntVal + BigInteger.DivRem(remainder, bMag, out remainder);
            }

            return new BigDecimal(outIntVal, scale);
        }

        public static BigDecimal operator -(BigDecimal a)
        {
            return new BigDecimal(BigInteger.Negate(a.IntVal), a.Scale);
        }


        /// <summary>
        /// Try to parse a text representation.
        /// </summary>
        /// <param name="text">Text form.</param>
        /// <param name="result">The output as a big decimal object.</param>
        /// <returns>True if parsing is successful, false otherwise.</returns>
        public static bool TryParse(string text, out BigDecimal result) => TryParse(text.AsSpan(), out result);

        /// <summary>
        /// Try to parse a text representation.
        /// </summary>
        /// <param name="text">Text form.</param>
        /// <param name="result">The output as a big decimal object.</param>
        /// <returns>True if parsing is successful, false otherwise.</returns>
        public static bool TryParse(ReadOnlySpan<char> text, out BigDecimal result)
        {
            try
            {
                result = Parse(text);
                return true;
            }
            catch (FormatException)
            {
                result = default;
                return false;
            }
            catch (OverflowException)
            {
                result = default;
                return false;
            }
        }

        /// <summary>
        /// Parse a text representation.
        /// </summary>
        /// <param name="text">Text form.</param>
        /// <returns>A big decimal object.</returns>
        /// <exception cref="FormatException">If the text is in an invalid format.</exception>
        /// <exception cref="OverflowException">If the text value is too large.</exception>
        public static BigDecimal Parse(string text) => Parse(text.AsSpan());

        /// <summary>
        /// Parse a text representation.
        /// </summary>
        /// <param name="text">Text form.</param>
        /// <returns>A big decimal object.</returns>
        /// <exception cref="FormatException">If the text is in an invalid format.</exception>
        /// <exception cref="OverflowException">If the text value is too large.</exception>
        public static BigDecimal Parse(ReadOnlySpan<char> text)
        {
            FormatException newFormatException(ReadOnlySpan<char> t) => new FormatException($"Invalid decimal: {t.ToString()}");
            const short sValStart = 0, sValInt = 1, sValDecimal = 2, sExpStart = 3, sExp = 4;

            var valNegative = false;
            var expNegative = false;
            var started = false;
            var state = sValStart;
            BigInteger intVal = 0;
            var scale = 0;
            var exponent = 0;
            foreach (var c in text)
            {
                switch (state)
                {
                    default:
                        //should never happen
                        throw new Exception();
                    case sValStart:
                        if (c == '-')
                        {
                            if (started)
                            {
                                throw newFormatException(text);
                            }

                            started = true;
                            valNegative = true;
                            break;
                        }
                        else if (c == '+')
                        {
                            if (started)
                            {
                                throw newFormatException(text);
                            }

                            started = true;
                            break;
                        }

                        state = sValInt;
                        goto case sValInt;
                    case sValInt:
                        if (c == '.')
                        {
                            state = sValDecimal;
                            break;
                        }

                        if (c == 'd' || c == 'D')
                        {
                            started = false;
                            state = sExpStart;
                            break;
                        }

                        if (!char.IsDigit(c))
                        {
                            throw newFormatException(text);
                        }

                        intVal = intVal * 10 + (valNegative ? '0' - c : c - '0');
                        break;
                    case sValDecimal:
                        if (c == 'd' || c == 'D')
                        {
                            started = false;
                            state = sExpStart;
                            break;
                        }

                        if (!char.IsDigit(c))
                        {
                            throw newFormatException(text);
                        }

                        intVal = intVal * 10 + (valNegative ? '0' - c : c - '0');
                        scale++;
                        break;
                    case sExpStart:
                        if (c == '-')
                        {
                            if (started)
                            {
                                throw newFormatException(text);
                            }

                            started = true;

                            expNegative = true;
                            break;
                        }
                        else if (c == '+')
                        {
                            if (started)
                            {
                                throw newFormatException(text);
                            }

                            started = true;
                            break;
                        }

                        state = sExp;
                        goto case sExp;
                    case sExp:
                        if (!char.IsDigit(c))
                        {
                            throw newFormatException(text);
                        }

                        exponent = exponent * 10 + (expNegative ? '0' - c : c - '0');
                        break;
                }
            }

            if (state == sValStart || state == sExpStart)
            {
                throw newFormatException(text);
            }

            if (intVal == 0 && valNegative)
            {
                return NegativeZero(scale);
            }

            return new BigDecimal(intVal, scale - exponent);
        }

        public override string ToString()
        {
            if (IsNegativeZero)
            {
                return "-0d0";
            }

            var sb = new StringBuilder(IntVal.ToString(CultureInfo.InvariantCulture));
            if (Scale == 0)
            {
                sb.Append(".0");
            }
            else if (Scale < 0)
            {
                //write the string as {mag}d{-scale}
                sb.Append('d');
                sb.Append(-Scale);
            }
            else
            {
                var smallestDotIdx = sb[0] == '-' ? 2 : 1;
                if (sb.Length - Scale >= smallestDotIdx)
                {
                    sb.Insert(sb.Length - Scale, '.');
                }
                else
                {
                    var d = Scale - (sb.Length - smallestDotIdx);
                    sb.Insert(smallestDotIdx, '.');
                    sb.Append('d');
                    sb.Append(-d);
                }
            }

            return sb.ToString();
        }

        public static BigDecimal NegativeZero(int scale = 0) => new BigDecimal(0, scale, true);

        public static BigDecimal Zero(int scale = 0) => new BigDecimal(0, scale);
    }
}
