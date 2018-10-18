using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace IonDotnet.Utils
{
    /// <summary>
    /// This class support converting between UTF-8 encoding and C# UTF-16 char representation
    /// </summary>
    internal static class Characters
    {
        private const int UnicodeTwoByteHeader = 0xC0; // 8 + 4 = 12 = 0xC0
        private const int UnicodeThreeByteHeader = 0xE0; // 8+4+2 = 14 = 0xE0
        private const int UnicodeFourByteHeader = 0xF0; // 8+4+2+1 = 15 = 0xF0
        private const int UnicodeContinuationByteHeader = 0x80;
        private const int UnicodeTwoByteMask = 0x1F; // 8-3 = 5 bits
        private const int UnicodeThreeByteMask = 0x0F; // 4 bits
        private const int UnicodeFourByteMask = 0x07; // 3 bits
        private const int UnicodeContinuationByteMask = 0x3F; // 6 bits in each continuation

        private const int MaximumUtf16OneCharCodePoint = 0x0000FFFF;
        private const int SurrogateMask = unchecked((int) 0xFFFFFC00);
        private const int SurrogateOffset = 0x00010000;
        private const int HighSurrogate = 0x0000D800; // 0b 1101 1000 0000 0000
        private const int LowSurrogate = 0x0000DC00; // 0b 1101 1100 0000 0000
        private const int SurrogateValueMask = (int) ~0xFFFFFC00;
        private const int SurrogateUtf32Shift = 10;
        private const int SurrogateUtf32Offset = 0x10000;

        public enum ProhibitionContext
        {
            ShortChar,
            LongChar,
            None
        }

        public static bool IsProhibited(int c, ProhibitionContext context)
        {
            if (context == ProhibitionContext.None)
                return false;
            var isControl = c <= 0x1F && 0x00 <= c;
            var isWhiteSpace = c == 0x09 // tab
                               || c == 0x0B // vertical tab
                               || c == 0x0C // form feed
                               || c == 0x20; // space 
            if (context == ProhibitionContext.ShortChar)
                return isControl && !isWhiteSpace;
            var isNewLine = c == 0x0A || c == 0x0D;
            return isControl && !isWhiteSpace && !isNewLine;
        }

        // these help convert from UTF-16 to Unicode Scalars (aka unicode code
        // points (aka characters)) which are "32" bit values (really just 21 bits)
        // the DON'T check validity of their input, they expect that to have happened
        // already.  This is a perf issue since normally this check has been done
        // to detect that these routines should be called at all - no need to do it
        // twice.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MakeUnicodeScalar(int highSurrogate, int lowSurrogate)
        {
            var c = (highSurrogate & SurrogateValueMask) << SurrogateUtf32Shift;
            c |= lowSurrogate & SurrogateValueMask;
            c += SurrogateUtf32Offset;
            return c;
        }

        /// <summary>
        /// Put these 2 UTF-8 bytes to a C# <see cref="char"/>
        /// </summary>
        /// <param name="b1">Byte 1</param>
        /// <param name="b2">Byte 2</param>
        /// <returns>2-byte char, since this is a 2-byte utf8</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char Utf8TwoByteScalar(int b1, int b2)
        {
            var c = ((b1 & UnicodeTwoByteMask) << 6) | (b2 & UnicodeContinuationByteMask);
            return (char) c;
        }

        /// <summary>
        /// Convert a 3-byte utf-8 code to 2 C# <see cref="char"/>
        /// </summary>
        /// <param name="b1">Byte 1</param>
        /// <param name="b2">Byte 2</param>
        /// <param name="b3">Byte 3</param>
        /// <returns>An int that is actually 2 C# chars</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Utf8ThreeByteScalar(int b1, int b2, int b3)
        {
            var c = ((b1 & UnicodeThreeByteMask) << 12)
                    | ((b2 & UnicodeContinuationByteMask) << 6)
                    | (b3 & UnicodeContinuationByteMask);
            return c;
        }

        /// <summary>
        /// Convert a 4-byte utf-8 code to 2 C# <see cref="char"/>
        /// </summary>
        /// <param name="b1">Byte 1</param>
        /// <param name="b2">Byte 2</param>
        /// <param name="b3">Byte 3</param>
        /// <param name="b4">Byte 4</param>
        /// <returns>An int that is actually 2 C# chars</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Utf8FourByteScalar(int b1, int b2, int b3, int b4)
        {
            var c = ((b1 & UnicodeFourByteMask) << 18)
                    | ((b2 & UnicodeContinuationByteMask) << 12)
                    | ((b3 & UnicodeContinuationByteMask) << 6)
                    | (b4 & UnicodeContinuationByteMask);
            return c;
        }

        /// <summary>
        /// Look at the first byte and tell how many bytes this UTF8 character is
        /// </summary>
        /// <param name="firstByte">First byte</param>
        /// <returns>Size of this UTF8 character</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetUtf8LengthFromFirstByte(int firstByte)
        {
            firstByte &= 0xff;
            if (IsOneByteUtf8(firstByte))
                return 1;
            if (IsTwoByteUtf8(firstByte))
                return 2;
            if (IsThreeByteUtf8(firstByte))
                return 3;
            if (IsFourByteUtf8(firstByte))
                return 4;
            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsOneByteUtf8(int b) => (b & 0x80) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsTwoByteUtf8(int b) => (b & ~UnicodeTwoByteMask) == UnicodeTwoByteHeader;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsThreeByteUtf8(int b) => (b & ~UnicodeThreeByteMask) == UnicodeThreeByteHeader;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsFourByteUtf8(int b) => (b & ~UnicodeFourByteMask) == UnicodeFourByteHeader;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NeedsSurrogateEncoding(int unicodeScalar) => unicodeScalar > MaximumUtf16OneCharCodePoint;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char GetHighSurrogate(int unicodeScalar)
        {
            Debug.Assert(unicodeScalar > MaximumUtf16OneCharCodePoint);
            var c = (unicodeScalar - SurrogateOffset) >> 10;
            return (char) ((c | HighSurrogate) & 0xffff);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char GetLowSurrogate(int unicodeScalar)
        {
            Debug.Assert(unicodeScalar > MaximumUtf16OneCharCodePoint);
            var c = (unicodeScalar - SurrogateOffset) & 0x3ff;
            return (char) ((c | LowSurrogate) & 0xffff);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is7BitChar(int c) => (c & ~0x7f) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is8BitChar(int c) => (c & ~0xff) == 0;

    }
}
