using System;
using System.Runtime.CompilerServices;
using System.Text;
using IonDotnet.Utils;

namespace IonDotnet.Internals.Text
{
    /// <summary>
    /// Contains constants that refer to escaped character sequence, could be newline or escaped
    /// </summary>
    internal static class CharacterSequence
    {
        public const int CharSeqEof = -1; // matches -1 (stream eof)
        public const int CharSeqStringTerminator = -2; // can't be >=0, ==-1 (eof), nor -2 (empty esc)
        public const int CharSeqStringNonTerminator = -3; // used for a pair of triple quotes treated a nothing

        /// <summary>
        /// Single new line '\n'
        /// </summary>
        public const int CharSeqNewlineSequence1 = -4;

        /// <summary>
        ///  Single carriage return '\r'
        /// </summary>
        public const int CharSeqNewlineSequence2 = -5;

        /// <summary>
        /// New line - carriage return pair '\r\n'
        /// </summary>
        public const int CharSeqNewlineSequence3 = -6;

        /// <summary>
        /// Escape followed by new line
        /// </summary>
        public const int CharSeqEscapedNewlineSequence1 = -7;

        /// <summary>
        /// Escape followed by carriage return
        /// </summary>
        public const int CharSeqEscapedNewlineSequence2 = -8;

        /// <summary>
        /// Escape followed by new line - carriage return pair
        /// </summary>
        public const int CharSeqEscapedNewlineSequence3 = -9;
    }

    /// <summary>
    /// Text-related constants
    /// </summary>
    internal static class TextConstants
    {
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
        public const int EscapeHex = -16;
        public const int EscapeBigU = -15;
        public const int EscapeLittleU = -14;
        private const int EscapeRemovesNewline2 = -13;
        private const int EscapeRemovesNewline = -12;

        #region Keywords

        public const int KeywordUnrecognized = -1;
        public const int KeywordNone = 0;
        public const int KeywordTrue = 1;
        public const int KeywordFalse = 2;
        public const int KeywordNull = 3;
        public const int KeywordBool = 4;
        public const int KeywordInt = 5;
        public const int KeywordFloat = 6;
        public const int KeywordDecimal = 7;
        public const int KeywordTimestamp = 8;
        public const int KeywordSymbol = 9;
        public const int KeywordString = 10;
        public const int KeywordBlob = 11;
        public const int KeywordClob = 12;
        public const int KeywordList = 13;
        public const int KeywordSexp = 14;
        public const int KeywordStruct = 15;
        public const int KeywordNan = 16;
        public const int KeywordSid = 17;

        #endregion

        #region KeywordBits

        public const int TnMaxNameLength = 10; //"TIMESTAMP".Length+ 1; // so anything too long will be 0

        #endregion

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
                    return EscapeRemovesNewline; // slash-new line the new line eater
                case '\r':
                    return EscapeRemovesNewline2; // slash-new line the new line eater
                case 'x':
                    return EscapeHex; //      \xHH  2-digit hexadecimal unicode character equivalent to \ u00HH
                case 'u':
                    return EscapeLittleU; //    any  \ uHHHH  4-digit hexadecimal unicode character
                case 'U':
                    return EscapeBigU;
            }
        }

        public static bool IsValidTerminatingCharForInf(int c)
        {
            if (Characters.Is8BitChar(c))
                return false;

            if (c >= 'a' && c <= 'z')
                return false;
            if (c >= 'A' && c <= 'Z')
                return false;
            if (c >= '0' && c <= '9')
                return false;
            if (c == '$' || c == '_')
                return false;

            return true;
        }

        // this can be faster but it's pretty unusual to be called.
        public static int TypeNameKeyWordFromMask(Span<int> readAhead, int readCount)
        {
            if (CompareName(readAhead, readCount, "int"))
                return KeywordInt;
            if (CompareName(readAhead, readCount, "blob"))
                return KeywordBlob;
            if (CompareName(readAhead, readCount, "clob"))
                return KeywordClob;
            if (CompareName(readAhead, readCount, "bool"))
                return KeywordBool;
            if (CompareName(readAhead, readCount, "float"))
                return KeywordFloat;
            if (CompareName(readAhead, readCount, "decimal"))
                return KeywordDecimal;
            if (CompareName(readAhead, readCount, "timestamp"))
                return KeywordTimestamp;
            if (CompareName(readAhead, readCount, "string"))
                return KeywordString;
            if (CompareName(readAhead, readCount, "symbol"))
                return KeywordSymbol;
            if (CompareName(readAhead, readCount, "sexp"))
                return KeywordSexp;
            if (CompareName(readAhead, readCount, "list"))
                return KeywordList;
            if (CompareName(readAhead, readCount, "struct"))
                return KeywordStruct;
            if (CompareName(readAhead, readCount, "null"))
                return KeywordNull;

            return KeywordUnrecognized;
        }

        private static bool CompareName(Span<int> readAhead, int readCount, string name)
        {
            if (name.Length != readCount)
                return false;

            for (var i = 0; i < readCount; i++)
            {
                if (name[i] != readAhead[i])
                    return false;
            }

            return true;
        }

        public static bool IsValidEscapeStart(int c)
            => GetEscapeReplacementCharacter(c & 0xff) != EscapeNotDefined && Characters.Is8BitChar(c);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWhiteSpace(int c) => c == ' ' || c == '\t' || c == '\n' || c == '\r';

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int HexDigitValue(int h)
        {
            if (h >= '0' && h <= '9')
                return h - '0';

            if (h >= 'a' && h <= 'f')
                return h - 'a' + 10;

            if (h >= 'A' && h <= 'F')
                return h - 'A' + 10;

            return -1;
        }

        public static bool IsValidSymbolCharacter(int c)
        {
            if (!Characters.Is8BitChar(c))
                return false;

            c &= 0xff;
            if (c == '$' || c == '_')
                return true;

            if (c >= 'a' && c <= 'z')
                return true;

            if (c >= 'A' && c <= 'Z')
                return true;

            if (c >= '0' && c <= '9')
                return true;

            return false;
        }

        public static bool IsValidExtendedSymbolCharacter(int c)
        {
            if (!Characters.Is8BitChar(c))
                return false;

            switch (c)
            {
                default:
                    return false;
                case '*':
                case '+':
                case '-':
                case '.':
                case '/':
                case ';':
                case '<':
                case '=':
                case '>':
                case '?':
                case '@':
                case '^':
                case '`':
                case '|':
                case '~':
                case '%':
                case '!':
                case '#':
                case '&':
                    return true;
            }
        }

        public static bool IsNumericStop(int codePoint)
        {
            switch (codePoint)
            {
                case -1:
                case '{':
                case '}':
                case '[':
                case ']':
                case '(':
                case ')':
                case ',':
                case '\"':
                case '\'':
                case ' ':
                case '\t':
                case '\n':
                case '\r':
                    // Whitespace
                    // case '/': // we check start of comment in the caller where we
                    //              can peek ahead for the following slash or asterisk
                    return true;
                default:
                    return false;
            }
        }

        public static IonType GetIonTypeOfToken(int token)
        {
            switch (token)
            {
                case TokenInt:
                case TokenBinary:
                case TokenHex:
                    return IonType.Int;
                case TokenDecimal:
                    return IonType.Decimal;
                case TokenFloat:
                    return IonType.Float;
                case TokenTimestamp:
                    return IonType.Timestamp;
                case TokenSymbolIdentifier:
                case TokenSymbolQuoted:
                case TokenSymbolOperator:
                    return IonType.Symbol;
                case TokenStringDoubleQuote:
                case TokenStringTripleQuote:
                    return IonType.String;
                default:
                    return IonType.None;
            }
        }

        public static int GetKeyword(StringBuilder word, int startWord, int endWord)
        {
            int c = word[startWord];
            var len = endWord - startWord; // +1 but we build that into the constants below
            switch (c)
            {
                case '$':
                    if (len > 1)
                    {
                        for (var i = startWord + 1; i < endWord; i++)
                        {
                            if (!char.IsDigit(word[i])) return -1;
                        }

                        return KeywordSid;
                    }

                    return -1;
                case 'b':
                    if (len == 4)
                    {
                        if (word[startWord + 1] == 'o'
                            && word[startWord + 2] == 'o'
                            && word[startWord + 3] == 'l'
                        )
                        {
                            return KeywordBool;
                        }

                        if (word[startWord + 1] == 'l'
                            && word[startWord + 2] == 'o'
                            && word[startWord + 3] == 'b'
                        )
                        {
                            return KeywordBlob;
                        }
                    }

                    return -1;
                case 'c':
                    if (len == 4)
                    {
                        if (word[startWord + 1] == 'l'
                            && word[startWord + 2] == 'o'
                            && word[startWord + 3] == 'b'
                        )
                        {
                            return KeywordClob;
                        }
                    }

                    return -1;
                case 'd':
                    if (len == 7)
                    {
                        if (word[startWord + 1] == 'e'
                            && word[startWord + 2] == 'c'
                            && word[startWord + 3] == 'i'
                            && word[startWord + 4] == 'm'
                            && word[startWord + 5] == 'a'
                            && word[startWord + 6] == 'l'
                        )
                        {
                            return KeywordDecimal;
                        }
                    }

                    return -1;
                case 'f':
                    if (len == 5)
                    {
                        if (word[startWord + 1] == 'a'
                            && word[startWord + 2] == 'l'
                            && word[startWord + 3] == 's'
                            && word[startWord + 4] == 'e')
                        {
                            return KeywordFalse;
                        }

                        if (word[startWord + 1] == 'l'
                            && word[startWord + 2] == 'o'
                            && word[startWord + 3] == 'a'
                            && word[startWord + 4] == 't')
                        {
                            return KeywordFloat;
                        }
                    }

                    return -1;
                case 'i':
                    if (len == 3)
                    {
                        if (word[startWord + 1] == 'n')
                        {
                            if (word[startWord + 2] == 't')
                            {
                                return KeywordInt;
                            }
                        }
                    }

                    return -1;
                case 'l':
                    if (len == 4)
                    {
                        if (word[startWord + 1] == 'i'
                            && word[startWord + 2] == 's'
                            && word[startWord + 3] == 't')
                        {
                            return KeywordList;
                        }
                    }

                    return -1;
                case 'n':
                    if (len == 4)
                    {
                        if (word[startWord + 1] == 'u'
                            && word[startWord + 2] == 'l'
                            && word[startWord + 3] == 'l')
                        {
                            return KeywordNull;
                        }
                    }
                    else if (len == 3)
                    {
                        if (word[startWord + 1] == 'a'
                            && word[startWord + 2] == 'n')
                        {
                            return KeywordNan;
                        }
                    }

                    return -1;
                case 's':
                    if (len == 4)
                    {
                        if (word[startWord + 1] == 'e'
                            && word[startWord + 2] == 'x'
                            && word[startWord + 3] == 'p')
                        {
                            return KeywordSexp;
                        }
                    }
                    else if (len == 6)
                    {
                        if (word[startWord + 1] == 't'
                            && word[startWord + 2] == 'r'
                        )
                        {
                            if (word[startWord + 3] == 'i'
                                && word[startWord + 4] == 'n'
                                && word[startWord + 5] == 'g')
                            {
                                return KeywordString;
                            }

                            if (word[startWord + 3] == 'u'
                                && word[startWord + 4] == 'c'
                                && word[startWord + 5] == 't')
                            {
                                return KeywordStruct;
                            }

                            return -1;
                        }

                        if (word[startWord + 1] == 'y'
                            && word[startWord + 2] == 'm'
                            && word[startWord + 3] == 'b'
                            && word[startWord + 4] == 'o'
                            && word[startWord + 5] == 'l')
                        {
                            return KeywordSymbol;
                        }
                    }

                    return -1;
                case 't':
                    if (len == 4)
                    {
                        if (word[startWord + 1] == 'r'
                            && word[startWord + 2] == 'u'
                            && word[startWord + 3] == 'e')
                        {
                            return KeywordTrue;
                        }
                    }
                    else if (len == 9)
                    {
                        if (word[startWord + 1] == 'i'
                            && word[startWord + 2] == 'm'
                            && word[startWord + 3] == 'e'
                            && word[startWord + 4] == 's'
                            && word[startWord + 5] == 't'
                            && word[startWord + 6] == 'a'
                            && word[startWord + 7] == 'm'
                            && word[startWord + 8] == 'p')
                        {
                            return KeywordTimestamp;
                        }
                    }

                    return -1;
                default:
                    return -1;
            }
        }

        public static int DecodeSid(StringBuilder sb)
        {
            var digits = sb.ToString(1, sb.Length - 1);
            return int.Parse(digits, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
