using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace IonDotnet.Utils
{
    /// <summary>
    /// This class support converting between UTF-8 encoding and C# UTF-16 char representation
    /// </summary>
    internal static class Characters
    {
        private const int UNICODE_TWO_BYTE_HEADER = 0xC0; // 8 + 4 = 12 = 0xC0
        private const int UNICODE_THREE_BYTE_HEADER = 0xE0; // 8+4+2 = 14 = 0xE0
        private const int UNICODE_FOUR_BYTE_HEADER = 0xF0; // 8+4+2+1 = 15 = 0xF0
        private const int UNICODE_CONTINUATION_BYTE_HEADER = 0x80;
        private const int UNICODE_TWO_BYTE_MASK = 0x1F; // 8-3 = 5 bits
        private const int UNICODE_THREE_BYTE_MASK = 0x0F; // 4 bits
        private const int UNICODE_FOUR_BYTE_MASK = 0x07; // 3 bits
        private const int UNICODE_CONTINUATION_BYTE_MASK = 0x3F; // 6 bits in each continuation

        private const int MAXIMUM_UTF16_1_CHAR_CODE_POINT = 0x0000FFFF;
        private const int SURROGATE_OFFSET = 0x00010000;
        private const int HIGH_SURROGATE = 0x0000D800; // 0b 1101 1000 0000 0000
        private const int LOW_SURROGATE = 0x0000DC00; // 0b 1101 1100 0000 0000

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

        /// <summary>
        /// Put these 2 UTF-8 bytes to a C# <see cref="char"/>
        /// </summary>
        /// <param name="b1">Byte 1</param>
        /// <param name="b2">Byte 2</param>
        /// <returns>2-byte char, since this is a 2-byte utf8</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char Utf8TwoByteScalar(int b1, int b2)
        {
            var c = ((b1 & UNICODE_TWO_BYTE_MASK) << 6) | (b2 & UNICODE_CONTINUATION_BYTE_MASK);
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
            var c = ((b1 & UNICODE_THREE_BYTE_MASK) << 12)
                    | ((b2 & UNICODE_CONTINUATION_BYTE_MASK) << 6)
                    | (b3 & UNICODE_CONTINUATION_BYTE_MASK);
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
            var c = ((b1 & UNICODE_FOUR_BYTE_MASK) << 18)
                    | ((b2 & UNICODE_CONTINUATION_BYTE_MASK) << 12)
                    | ((b3 & UNICODE_CONTINUATION_BYTE_MASK) << 6)
                    | (b4 & UNICODE_CONTINUATION_BYTE_MASK);
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
        public static bool IsOneByteUtf8(int b) => (b & 0x80) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsTwoByteUtf8(int b) => (b & ~UNICODE_TWO_BYTE_MASK) == UNICODE_TWO_BYTE_HEADER;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsThreeByteUtf8(int b) => (b & ~UNICODE_THREE_BYTE_MASK) == UNICODE_THREE_BYTE_HEADER;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsFourByteUtf8(int b) => (b & ~UNICODE_FOUR_BYTE_MASK) == UNICODE_FOUR_BYTE_HEADER;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NeedsSurrogateEncoding(int unicodeScalar) => unicodeScalar > MAXIMUM_UTF16_1_CHAR_CODE_POINT;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char HighSurrogate(int unicodeScalar)
        {
            Debug.Assert(unicodeScalar > MAXIMUM_UTF16_1_CHAR_CODE_POINT);
            var c = (unicodeScalar - SURROGATE_OFFSET) >> 10;
            return (char) ((c | HIGH_SURROGATE) & 0xffff);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char LowSurrogate(int unicodeScalar)
        {
            Debug.Assert(unicodeScalar > MAXIMUM_UTF16_1_CHAR_CODE_POINT);
            var c = (unicodeScalar - SURROGATE_OFFSET) & 0x3ff;
            return (char) ((c | LOW_SURROGATE) & 0xffff);
        }
    }
}
