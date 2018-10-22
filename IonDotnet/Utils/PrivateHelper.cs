using System;

namespace IonDotnet.Utils
{
    internal static class PrivateHelper
    {
        public static readonly string[] EmptyStringArray = new string[0];

        internal static bool IsNegativeZero(double d)
        {
            //check the first bit
            const int shift = (8 * sizeof(double)) - 1;
            return d == 0 && BitConverter.DoubleToInt64Bits(d) >> shift != 0;
        }
    }
}
