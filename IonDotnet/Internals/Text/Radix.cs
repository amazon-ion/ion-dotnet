using System;

namespace IonDotnet.Internals.Text
{
    internal enum Radix
    {
        Binary,
        Decimal,
        Hex
    }

    internal static class RadixExtensions
    {
        public static bool IsPrefix(this Radix radix, int c)
        {
            switch (radix)
            {
                case Radix.Binary:
                    return c == 'b' || c == 'B';
                case Radix.Decimal:
                    return false;
                case Radix.Hex:
                    return c == 'x' || c == 'X';
                default:
                    throw new ArgumentOutOfRangeException(nameof(radix), radix, null);
            }
        }

        public static bool IsValidDigit(this Radix radix, int c)
        {
            switch (radix)
            {
                case Radix.Binary:
                    return c == '0' || c == '1';
                case Radix.Decimal:
                    return char.IsDigit((char) c);
                case Radix.Hex:
                    return c >= '0' && c <= '9'
                           || c >= 'a' && c <= 'f'
                           || c >= 'A' && c <= 'F';
                default:
                    throw new ArgumentOutOfRangeException(nameof(radix), radix, null);
            }
        }

        public static char NormalizeDigit(this Radix radix, int c)
        {
            switch (radix)
            {
                case Radix.Binary:
                    return (char) c;
                case Radix.Decimal:
                    return (char) c;
                case Radix.Hex:
                    return char.ToLower((char) c);
                default:
                    throw new ArgumentOutOfRangeException(nameof(radix), radix, null);
            }
        }
    }
}
