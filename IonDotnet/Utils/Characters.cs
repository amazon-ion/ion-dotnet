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
        private const int SURROGATE_MASK = unchecked((int) 0xFFFFFC00);
        private const int SURROGATE_OFFSET = 0x00010000;
        private const int HIGH_SURROGATE = 0x0000D800; // 0b 1101 1000 0000 0000
        private const int LOW_SURROGATE = 0x0000DC00; // 0b 1101 1100 0000 0000
        private const int surrogate_value_mask = (int) ~0xFFFFFC00;
        private const int surrogate_utf32_shift = 10;
        private const int surrogate_utf32_offset = 0x10000;

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

        // these help convert from Java UTF-16 to Unicode Scalars (aka unicode code
        // points (aka characters)) which are "32" bit values (really just 21 bits)
        // the DON'T check validity of their input, they expect that to have happened
        // already.  This is a perf issue since normally this check has been done
        // to detect that these routines should be called at all - no need to do it
        // twice.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MakeUnicodeScalar(int highSurrogate, int lowSurrogate)
        {
            var c = (highSurrogate & surrogate_value_mask) << surrogate_utf32_shift;
            c |= lowSurrogate & surrogate_value_mask;
            c += surrogate_utf32_offset;
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
        public static char GetHighSurrogate(int unicodeScalar)
        {
            Debug.Assert(unicodeScalar > MAXIMUM_UTF16_1_CHAR_CODE_POINT);
            var c = (unicodeScalar - SURROGATE_OFFSET) >> 10;
            return (char) ((c | HIGH_SURROGATE) & 0xffff);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char GetLowSurrogate(int unicodeScalar)
        {
            Debug.Assert(unicodeScalar > MAXIMUM_UTF16_1_CHAR_CODE_POINT);
            var c = (unicodeScalar - SURROGATE_OFFSET) & 0x3ff;
            return (char) ((c | LOW_SURROGATE) & 0xffff);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is7BitChar(int c) => (c & ~0x7f) == 0;

        public const int EscapeNotDefined = -11;
        public const int ESCAPE_HEX = -16;
        public const int ESCAPE_BIG_U = -15;
        public const int ESCAPE_LITTLE_U = -14;
        private const int ESCAPE_REMOVES_NEWLINE2 = -13;
        private const int ESCAPE_REMOVES_NEWLINE = -12;

        public static int GetEscapeReplacementCharacter(int c)
        {
            switch (c)
            {
                default:
                    return EscapeNotDefined;
                case '0':
                    return 0; //    \u0000  \0  alert NUL
                case 'a':
                    return 7; //    \u0007  \a  alert BEL
                case 'b':
                    return 8; //    \u0008  \b  backspace BS
                case 't':
                    return 9; //    \u0009  \t  horizontal tab HT
                case 'n':
                    return '\n'; //    \ u000A  \ n  linefeed LF
                case 'f':
                    return 0x0c; //    \u000C  \f  form feed FF
                case 'r':
                    return '\r'; //    \ u000D  \ r  carriage return CR
                case 'v':
                    return 0x0b; //    \u000B  \v  vertical tab VT
                case '"':
                    return '"'; //    \u0022  \"  double quote
                case '\'':
                    return '\''; //    \u0027  \'  single quote
                case '?':
                    return '?'; //    \u003F  \?  question mark
                case '\\':
                    return '\\'; //    \u005C  \\  backslash
                case '/':
                    return '/'; //    \u002F  \/  forward slash nothing  \NL  escaped NL expands to nothing
                case '\n':
                    return ESCAPE_REMOVES_NEWLINE; // slash-new line the new line eater
                case '\r':
                    return ESCAPE_REMOVES_NEWLINE2; // slash-new line the new line eater
                case 'x':
                    return ESCAPE_HEX; //      \xHH  2-digit hexadecimal unicode character equivalent to \ u00HH
                case 'u':
                    return ESCAPE_LITTLE_U; //    any  \ uHHHH  4-digit hexadecimal unicode character
                case 'U':
                    return ESCAPE_BIG_U;
            }
        }
    }
}
