namespace IonDotnet.Internals.Text
{
    internal class TextConstants
    {
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
    }
}
