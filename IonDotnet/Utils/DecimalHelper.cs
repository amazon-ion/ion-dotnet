using System;
using System.Diagnostics;

namespace IonDotnet.Utils
{
    internal static class DecimalHelper
    {
        /// <summary>
        /// Copy the byte representation of a decimal to the buffer in big-endian byte order (most significant byte first).
        /// </summary>
        /// <param name="bytes">Byte buffer.</param>
        /// <param name="value">Decimal value.</param>
        /// <returns>The end index of the buffer segment that contains meaningful data.</returns>
        public static unsafe int CopyDecimalBigEndian(Span<byte> bytes, decimal value)
        {
            Debug.Assert(bytes.Length >= sizeof(decimal));

            var p = (byte*) &value;

            //keep the flag the same
            bytes[0] = p[0];
            bytes[1] = p[1];
            bytes[2] = p[2];
            bytes[3] = p[3];

            //high
            var i = 7;
            while (i > 3 && p[i] == 0)
            {
                i--;
            }

            var hasHigh = i > 3;
            var j = 3;
            while (i > 3)
            {
                bytes[++j] = p[i--];
            }

            //mid
            i = 15;
            bool hasMid;
            if (!hasHigh)
            {
                while (i > 11 && p[i] == 0)
                {
                    i--;
                }

                hasMid = i > 11;
            }
            else
            {
                hasMid = true;
            }

            while (i > 11)
            {
                bytes[++j] = p[i--];
            }

            //lo
            i = 11;
            if (!hasMid)
            {
                while (i > 7 && p[i] == 0)
                {
                    i--;
                }
            }

            while (i > 7)
            {
                bytes[++j] = p[i--];
            }

            return j;
        }
    }
}
