using System;
using System.Runtime.CompilerServices;
using IonDotnet.Utils;

namespace IonDotnet.Internals.Text
{
    internal class TextConstants
    {
        /// <summary>
        /// Contains constants that refer to escaped character sequence, could be newline or escaped
        /// </summary>
        public static class CharacterSequence
        {
            public const int CharSeqEof = -1; // matches -1 (stream eof)
            public const int CharSeqStringTerminator = -2; // can't be >=0, ==-1 (eof), nor -2 (empty esc)
            public const int CharSeqStringNonTerminator = -3; // used for a pair of triple quotes treated a nothing

            public const int CharSeqNewlineSequence1 = -4; // single new line
            public const int CharSeqNewlineSequence2 = -5; // single carriage return
            public const int CharSeqNewlineSequence3 = -6; // new line - carriage return pair
            public const int CharSeqEscapedNewlineSequence1 = -7; // escape followed by new line
            public const int CharSeqEscapedNewlineSequence2 = -8; // escape followed by carriage return
            public const int CharSeqEscapedNewlineSequence3 = -9; // escape followed by new line - carriage return pair
        }

        #region Tokens

        /**
         * A bunch of tokens const, note that these are used for reference, not the
         * actual ASCII values
         */
        
        public const int TokenError = -1;
        public const int TokenEof = 0;

        public const int TokenUnknownNumeric = 1;
        public const int TokenInt = 2;
        public const int TokenHex = 3;
        public const int TokenDecimal = 4;
        public const int TokenFloat = 5;
        public const int TokenFloatInf = 6;
        public const int TokenFloatMinusInf = 7;
        public const int TokenTimestamp = 8;

        /**
         * Unquoted identifier symbol, including keywords like {@code true} and
         * {@code nan} as well as SIDs like {@code $123}
         */
        public const int TokenSymbolIdentifier = 9;

        /**
         * Single-quoted symbol
         */
        public const int TokenSymbolQuoted = 10;

        /**
         * Unquoted operator sequence for sexp
         */
        public const int TokenSymbolOperator = 11;
        public const int TokenStringDoubleQuote = 12;
        public const int TokenStringTripleQuote = 13;
        public const int TokenDot = 14;
        public const int TokenComma = 15;
        public const int TokenColon = 16;
        public const int TokenDoubleColon = 17;
        public const int TokenOpenParen = 18;
        public const int TokenCloseParen = 19;
        public const int TokenOpenBrace = 20;
        public const int TokenCloseBrace = 21;
        public const int TokenOpenSquare = 22;
        public const int TokenCloseSquare = 23;
        public const int TokenOpenDoubleBrace = 24;
        public const int TokenCloseDoubleBrace = 25;
        public const int TokenBinary = 26;
        public const int TokenMax = 26;

        #endregion

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

        public static bool IsValidEscapeStart(int c)
            => GetEscapeReplacementCharacter(c & 0xff) != EscapeNotDefined && Characters.Is8BitChar(c);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWhiteSpace(int c) => c == ' ' || c == '\t' || c == '\n' || c == '\r';

        public static int HexDigitValue(int c)
        {
            throw new NotImplementedException();
        }
    }
}
