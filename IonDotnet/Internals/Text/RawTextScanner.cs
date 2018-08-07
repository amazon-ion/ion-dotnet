using System;
using System.Diagnostics;
using System.Text;
using IonDotnet.Systems;
using IonDotnet.Utils;

namespace IonDotnet.Internals.Text
{
    /// <summary>
    /// This class is responsible for the main parsing/reading logic, which works directly with the
    /// <see cref="TextStream"/> abstraction to scan the input for interesting token as well as provide
    /// the method for reading the value.
    /// </summary>
    /// <remarks>
    /// At any point during the parsing, there is one active _token type that represent the current state
    /// of the input. The raw text reader class can based on that token to read/load value, or to perform
    /// actions such as skipping to next token.
    /// </remarks>
    internal sealed class RawTextScanner
    {
        private enum NumericState
        {
            Start,
            Underscore,
            Digit,
        }

        private enum CommentStrategy
        {
            Ignore,
            Error,
            Break
        }

        private readonly TextStream _input;
        private int _base64PrefetchCount;
        public int Token { get; private set; } = -1;

        public bool UnfinishedToken { get; private set; }

        public RawTextScanner(TextStream input)
        {
            _input = input;
        }

        public int NextToken()
        {
            int token;
            var c = UnfinishedToken ? SkipToEnd() : SkipOverWhiteSpace(CommentStrategy.Ignore);

            UnfinishedToken = true;

            switch (c)
            {
                default:
                    throw new InvalidTokenException(c);
                case -1:
                    return FinishNextToken(TextConstants.TokenEof, true);
                case '/':
                    UnreadChar(c);
                    return FinishNextToken(TextConstants.TokenSymbolOperator, true);
                case ':':
                    var c2 = ReadChar();
                    if (c2 == ':')
                        return FinishNextToken(TextConstants.TokenDoubleColon, true);

                    UnreadChar(c2);
                    return FinishNextToken(TextConstants.TokenColon, true);
                case '{':
                    c2 = ReadChar();
                    if (c2 == '{')
                        return FinishNextToken(TextConstants.TokenOpenDoubleBrace, true);
                    UnreadChar(c2);
                    return FinishNextToken(TextConstants.TokenOpenBrace, true);
                case '}':
                    // detection of double closing braces is done
                    // in the parser in the blob and clob handling
                    // state - it's otherwise ambiguous with closing
                    // two structs together. see tryForDoubleBrace() below
                    return FinishNextToken(TextConstants.TokenCloseBrace, false);
                case '[':
                    return FinishNextToken(TextConstants.TokenOpenSquare, true);
                case ']':
                    return FinishNextToken(TextConstants.TokenCloseBrace, false);
                case '(':
                    return FinishNextToken(TextConstants.TokenOpenParen, true);
                case ')':
                    return FinishNextToken(TextConstants.TokenCloseParen, false);
                case ',':
                    return FinishNextToken(TextConstants.TokenComma, false);
                case '.':
                    //TODO peek?
                    c2 = ReadChar();
                    UnreadChar(c2);
                    if (!TextConstants.IsValidExtendedSymbolCharacter(c2))
                        return FinishNextToken(TextConstants.TokenDot, false);
                    UnreadChar('.');
                    return FinishNextToken(TextConstants.TokenSymbolOperator, true);
                case '\'':
                    if (Is2SingleQuotes())
                        return FinishNextToken(TextConstants.TokenStringTripleQuote, true);
                    return FinishNextToken(TextConstants.TokenSymbolQuoted, true);
                case '+':
                    if (PeekInf(c))
                        return FinishNextToken(TextConstants.TokenFloatInf, false);
                    UnreadChar(c);
                    return FinishNextToken(TextConstants.TokenSymbolOperator, true);
                case '#':
                case '<':
                case '>':
                case '*':
                case '=':
                case '^':
                case '&':
                case '|':
                case '~':
                case ';':
                case '!':
                case '?':
                case '@':
                case '%':
                case '`':
                    UnreadChar(c);
                    return FinishNextToken(TextConstants.TokenSymbolOperator, true);
                case '"':
                    return FinishNextToken(TextConstants.TokenStringDoubleQuote, true);
                case 'a':
                case 'b':
                case 'c':
                case 'd':
                case 'e':
                case 'f':
                case 'g':
                case 'h':
                case 'i':
                case 'j':
                case 'k':
                case 'l':
                case 'm':
                case 'n':
                case 'o':
                case 'p':
                case 'q':
                case 'r':
                case 's':
                case 't':
                case 'u':
                case 'v':
                case 'w':
                case 'x':
                case 'y':
                case 'z':
                case 'A':
                case 'B':
                case 'C':
                case 'D':
                case 'E':
                case 'F':
                case 'G':
                case 'H':
                case 'I':
                case 'J':
                case 'K':
                case 'L':
                case 'M':
                case 'N':
                case 'O':
                case 'P':
                case 'Q':
                case 'R':
                case 'S':
                case 'T':
                case 'U':
                case 'V':
                case 'W':
                case 'X':
                case 'Y':
                case 'Z':
                case '$':
                case '_':
                    UnreadChar(c);
                    return FinishNextToken(TextConstants.TokenSymbolIdentifier, true);
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    token = ScanForNumericType(c);
                    UnreadChar(c);
                    return FinishNextToken(token, true);
                case '-':
                    // see if we have a number or what might be an extended symbol
                    //TODO peek()
                    c2 = ReadChar();
                    UnreadChar(c2);
                    if (char.IsDigit((char) c2))
                    {
                        token = ScanForNegativeNumbericType(c);
                        UnreadChar(c);
                        return FinishNextToken(token, true);
                    }

                    // this will consume the inf if it succeeds
                    if (PeekInf(c))
                        return FinishNextToken(TextConstants.TokenFloatMinusInf, false);

                    UnreadChar(c);
                    return FinishNextToken(TextConstants.TokenSymbolOperator, true);
            }
        }

        /// <summary>
        /// Variant of <see cref="ScanForNumericType"/> where the passed in start
        /// character is '-'. 
        /// </summary>
        /// <param name="c">First character, should be '-'</param>
        /// <returns>Numeric token type</returns>
        /// <exception cref="InvalidTokenException">When an illegal token is encountered</exception>
        /// <remarks>This will unread the minus sign</remarks>
        private int ScanForNegativeNumbericType(int c)
        {
            Debug.Assert(c == '-');
            c = ReadChar();
            var t = ScanForNumericType(c);
            if (t == TextConstants.TokenTimestamp)
                throw new InvalidTokenException(c);

            // and the caller need to unread the '-'
            //TODO why c?
            UnreadChar(c);
            return t;
        }

        /// <summary>
        /// We encountered a numeric character (digit or minus), now we scan a little
        /// way ahead to spot some of the numeric types.
        /// <para>
        /// This only looks far enough (2-6 chars) to identify hex and timestamp,
        /// it might encounter chars like 'd' or 'e' and decide if this token is float
        /// or decimal (or int), but it might return TOKEN_UNKNOWN_NUMERIC
        /// </para>
        /// </summary>
        /// <param name="c1">First numeric char</param>
        /// <returns>Numeric token type</returns>
        /// <remarks>It will unread everything it reads, and the character passed in as the first digit</remarks>
        private int ScanForNumericType(int c1)
        {
            var t = TextConstants.TokenUnknownNumeric;
            Span<int> readChars = stackalloc int[6];
            var readCharCount = 0;
            Debug.Assert(char.IsDigit((char) c1));

            var c = ReadChar();
            readChars[readCharCount++] = c;
            if (c1 == '0')
            {
                //check for hex
                switch (c)
                {
                    default:
                        if (IsTerminatingCharacter(c))
                        {
                            t = TextConstants.TokenInt;
                        }

                        break;
                    case 'x':
                    case 'X':
                        t = TextConstants.TokenHex;
                        break;
                    case 'd':
                    case 'D':
                        t = TextConstants.TokenDecimal;
                        break;
                    case 'e':
                    case 'E':
                        t = TextConstants.TokenFloat;
                        break;
                    case 'b':
                    case 'B':
                        t = TextConstants.TokenBinary;
                        break;
                }
            }

            if (t == TextConstants.TokenUnknownNumeric)
            {
                if (char.IsDigit((char) c))
                {
                    //2nd digit
                    // it might be a timestamp if we have 4 digits, a dash,
                    // and a digit
                    c = ReadChar();
                    readChars[readCharCount++] = c;
                    if (char.IsDigit((char) c))
                    {
                        //digit 3
                        c = ReadChar();
                        readChars[readCharCount++] = c;
                        if (char.IsDigit((char) c))
                        {
                            //digit 4, year
                            c = ReadChar();
                            readChars[readCharCount++] = c;
                            if (c == '-' || c == 'T')
                            {
                                // we have dddd- or ddddT looks like a timestamp
                                // (or invalid input)
                                t = TextConstants.TokenTimestamp;
                            }
                        }
                    }
                }
            }

            // unread whatever we read, including the passed in char
            do
            {
                readCharCount--;
                c = readChars[readCharCount];
                UnreadChar(c);
            } while (readCharCount > 0);

            return t;
        }

        private bool IsTerminatingCharacter(int c)
        {
            switch (c)
            {
                default:
                    return TextConstants.IsNumericStop(c);
                case '/':
                    //TODO peek
                    c = ReadChar();
                    UnreadChar(c);
                    return c == '/' || c == '*';
                case CharacterSequence.CharSeqNewlineSequence1:
                case CharacterSequence.CharSeqNewlineSequence2:
                case CharacterSequence.CharSeqNewlineSequence3:
                case CharacterSequence.CharSeqEscapedNewlineSequence1:
                case CharacterSequence.CharSeqEscapedNewlineSequence2:
                case CharacterSequence.CharSeqEscapedNewlineSequence3:
                    return true;
            }
        }

        /// <summary>
        /// Turns out ion-text allows +inf and -inf
        /// </summary>
        private bool PeekInf(int c)
        {
            if (c != '+' && c != '-')
                return false;

            c = ReadChar();
            if (c == 'i')
            {
                c = ReadChar();
                if (c == 'n')
                {
                    c = ReadChar();
                    if (c == 'f')
                    {
                        c = ReadChar();
                        if (IsTerminatingCharacter(c))
                        {
                            UnreadChar(c);
                            return true;
                        }

                        UnreadChar(c);
                        c = 'f';
                    }

                    UnreadChar(c);
                    c = 'n';
                }

                UnreadChar(c);
                c = 'i';
            }

            UnreadChar(c);
            return false;
        }

        /// <summary>
        /// This peeks ahead to see if the next two characters are single quotes.
        /// This would finish off a triple quote when the first quote has been read. 
        /// </summary>
        /// <returns>True if the next two characters are single quotes</returns>
        /// <remarks>
        /// If it suceeds it will consume the 2 quotes.
        /// If it fails it will unread. 
        /// </remarks>
        private bool Is2SingleQuotes()
        {
            var c = ReadChar();
            if (c != '\'')
            {
                UnreadChar(c);
                return false;
            }

            c = ReadChar();
            if (c == '\'') return true;

            UnreadChar(c);
            UnreadChar('\'');
            return false;
        }

        private int FinishNextToken(int token, bool contentIsWaiting)
        {
            Token = token;
            UnfinishedToken = contentIsWaiting;
            return token;
        }

        /// <summary>
        /// Skip to the end of a token block
        /// </summary>
        /// <returns>New token</returns>
        /// <exception cref="InvalidTokenException">When the token read is unknown</exception>
        private int SkipToEnd()
        {
            int c;
            switch (Token)
            {
                case TextConstants.TokenUnknownNumeric:
                    c = SkipOverNumber();
                    break;
                case TextConstants.TokenInt:
                    c = SkipOverInt();
                    break;
                case TextConstants.TokenHex:
                    c = SkipOverRadix(Radix.Hex);
                    break;
                case TextConstants.TokenBinary:
                    c = SkipOverRadix(Radix.Binary);
                    break;
                case TextConstants.TokenDecimal:
                    c = SkipOverDecimal();
                    break;
                case TextConstants.TokenFloat:
                    c = SkipOverFloat();
                    break;
                case TextConstants.TokenTimestamp:
                    c = SkipOverTimestamp();
                    break;
                case TextConstants.TokenSymbolIdentifier:
                    c = SkipOverSymbolIdentifier();
                    break;
                case TextConstants.TokenSymbolQuoted:
                    Debug.Assert(!Is2SingleQuotes());
                    c = SkipSingleQuotedString();
                    break;
                case TextConstants.TokenSymbolOperator:
                    c = SkipOverSymbolOperator();
                    break;
                case TextConstants.TokenStringDoubleQuote:
                    SkipDoubleQuotedString();
                    c = SkipOverWhiteSpace(CommentStrategy.Ignore);
                    break;
                case TextConstants.TokenStringTripleQuote:
                    skip_triple_quoted_string();
                    c = SkipOverWhiteSpace(CommentStrategy.Ignore);
                    break;

                case TextConstants.TokenOpenDoubleBrace:
                    // works just like a pair of nested structs
                    // since "skip_over" doesn't care about formal
                    // syntax (like requiring field names);
                    skip_over_blob();
                    c = ReadChar();
                    break;
                case TextConstants.TokenOpenBrace:
                    SkipOverStruct();
                    c = ReadChar();
                    break;
                case TextConstants.TokenOpenParen:
                    SkipOverSexp(); // you can't save point a scanned sexp (right now anyway)
                    c = ReadChar();
                    break;
                case TextConstants.TokenOpenSquare:
                    SkipOverList(); // you can't save point a scanned list (right now anyway)
                    c = ReadChar();
                    break;
                default:
                    //Unknown token
                    throw new InvalidTokenException(Token);
            }

            if (TextConstants.IsWhiteSpace(c))
            {
                c = SkipOverWhiteSpace(CommentStrategy.Ignore);
            }

            UnfinishedToken = false;
            return c;
        }

        private void skip_over_blob()
        {
            throw new NotImplementedException();
        }

        public void SkipOverStruct()
        {
            throw new NotImplementedException();
        }

        public void SkipOverSexp()
        {
            throw new NotImplementedException();
        }

        private void skip_triple_quoted_string()
        {
            throw new NotImplementedException();
        }

        public void SkipOverList()
        {
            throw new NotImplementedException();
        }

        private int SkipOverSymbolOperator()
        {
            throw new NotImplementedException();
        }

        private int SkipSingleQuotedString()
        {
            throw new NotImplementedException();
        }

        private int SkipOverSymbolIdentifier()
        {
            var c = ReadChar();

            while (TextConstants.IsValidSymbolCharacter(c))
            {
                c = ReadChar();
            }

            return c;
        }

        private int SkipOverTimestamp()
        {
            throw new NotImplementedException();
        }

        private int SkipOverFloat() => SkipOverNumber();

        private int SkipOverDecimal() => SkipOverNumber();

        private int SkipOverRadix(Radix radix)
        {
            int c;

            c = ReadChar();
            if (c == '-')
            {
                c = ReadChar();
            }

            Debug.Assert(c == '0');
            c = ReadChar();
            Debug.Assert(radix.IsPrefix(c));

            //TODO is this ok?
//            c = ReadNumeric(NULL_APPENDABLE, radix);
            SkipOverNumber();

            if (!IsTerminatingCharacter(c))
                throw new InvalidTokenException(c);

            return c;
        }

        private int SkipOverInt()
        {
            var c = ReadChar();
            if (c == '-')
            {
                c = ReadChar();
            }

            c = SkipOverDigits(c);
            if (!IsTerminatingCharacter(c))
                throw new InvalidTokenException(c);

            return c;
        }

        private int SkipOverNumber()
        {
            var c = ReadChar();

            // first consume any leading 0 to get it out of the way
            if (c == '-')
            {
                c = ReadChar();
            }

            // could be a long int, a decimal, a float
            // it cannot be a hex or a valid timestamp
            // so scan digits - if decimal can more digits
            // if d or e eat possible sign
            // scan to end of digits
            c = SkipOverDigits(c);
            if (c == '.')
            {
                c = ReadChar();
                c = SkipOverDigits(c);
            }

            if (c == 'd' || c == 'D' || c == 'e' || c == 'E')
            {
                c = ReadChar();
                if (c == '-' || c == '+')
                {
                    c = ReadChar();
                }

                c = SkipOverDigits(c);
            }

            if (!IsTerminatingCharacter(c))
                throw new InvalidTokenException(c);

            return c;
        }

        private int SkipOverDigits(int c)
        {
            while (char.IsDigit((char) c))
            {
                c = ReadChar();
            }

            return c;
        }

        private bool OnComment(CommentStrategy commentStrategy)
        {
            //A '/' character has been found, so break the loop as it may be a valid blob character.
            if (commentStrategy == CommentStrategy.Break)
                return false;

            int next;
            //Skip over all of the comment's text.
            if (commentStrategy == CommentStrategy.Ignore)
            {
                next = ReadChar();
                switch (next)
                {
                    default:
                        UnreadChar(next);
                        return false;
                    case '/':
                        //valid comment
                        SkipSingleLineComment();
                        return true;
                    case '*':
                        //valid block comment
                        SkipBlockComment();
                        return true;
                }
            }

            //here means CommentStrategy.Error
            //If it's a valid comment, throw an error.
            next = ReadChar();
            if (next == '/' || next == '*')
                throw new InvalidTokenException("Illegal comment");

            UnreadChar(next);
            return false;
        }

        /// <summary>
        /// Must be called right after "/*"
        /// </summary>
        private void SkipBlockComment()
        {
            while (true)
            {
                var c = ReadChar();
                switch (c)
                {
                    case -1:
                        throw new InvalidTokenException("Bad start of comment token");
                    case '*':
                        // read back to back '*'s until you hit a '/' and terminate the comment
                        // or you see a non-'*'; in which case you go back to the outer loop.
                        // this just avoids the read-unread pattern on every '*' in a line of '*'
                        // commonly found at the top and bottom of block comments
                        while (true)
                        {
                            c = ReadChar();
                            if (c == '/')
                                return;
                            if (c != '*')
                                break;
                        }

                        break;
                }
            }
        }

        /// <summary>
        /// Must be called right after "//"
        /// </summary>
        private void SkipSingleLineComment()
        {
            while (true)
            {
                var c = ReadChar();
                switch (c)
                {
                    // new line normalization and counting is handled in read_char
                    case CharacterSequence.CharSeqNewlineSequence1:
                    case CharacterSequence.CharSeqNewlineSequence2:
                    case CharacterSequence.CharSeqNewlineSequence3:
                    case CharacterSequence.CharSeqEscapedNewlineSequence1:
                    case CharacterSequence.CharSeqEscapedNewlineSequence2:
                    case CharacterSequence.CharSeqEscapedNewlineSequence3:
                    case -1:
                        return;
                }

                //still in the comment, read another character
            }
        }

        /// <summary>
        /// Skip the whitespace and comments to the next token
        /// </summary>
        /// <returns>True if any whitespace is skipped</returns>
        public bool SkipWhiteSpace() => SkipWhiteSpaceWithCommentStrategy(CommentStrategy.Ignore);

        /// <summary>
        /// Skip whitespace
        /// </summary>
        /// <param name="commentStrategy">Comment strategy to apply</param>
        /// <returns>Next char(token) in the stream</returns>
        private int SkipOverWhiteSpace(CommentStrategy commentStrategy)
        {
            SkipWhiteSpaceWithCommentStrategy(commentStrategy);
            return ReadChar();
        }

        private bool SkipWhiteSpaceWithCommentStrategy(CommentStrategy commentStrategy)
        {
            var anyWhitespace = false;
            int c;
            while (true)
            {
                c = ReadChar();
                switch (c)
                {
                    default:
                        goto Done;
                    case ' ':
                    case '\t':
                    case CharacterSequence.CharSeqNewlineSequence1:
                    case CharacterSequence.CharSeqNewlineSequence2:
                    case CharacterSequence.CharSeqNewlineSequence3:
                    case CharacterSequence.CharSeqEscapedNewlineSequence1:
                    case CharacterSequence.CharSeqEscapedNewlineSequence2:
                    case CharacterSequence.CharSeqEscapedNewlineSequence3:
                        // new line normalization and counting is handled in ReadChar
                        anyWhitespace = true;
                        break;
                    case '/':
                        if (!OnComment(commentStrategy))
                        {
                            goto Done;
                        }

                        anyWhitespace = true;
                        break;
                }
            }

            Done:
            UnreadChar(c);
            return anyWhitespace;
        }

        /// <summary>
        /// This will, depending on the current unit c1, read all the remaining bytes and merge them into a 4-byte int
        /// </summary>
        /// <param name="c1"></param>
        /// <returns></returns>
        private int ReadLargeCharSequence(int c1)
        {
            if (_input.IsByteStream)
                return ReadUtf8Sequence(c1);

            if (char.IsHighSurrogate((char) c1))
            {
                var c2 = ReadChar();
                if (char.IsLowSurrogate((char) c2))
                {
                    c1 = Characters.MakeUnicodeScalar(c1, c2);
                }
            }

            return c1;
        }

        private int ReadUtf8Sequence(int c1)
        {
            var len = Characters.GetUtf8LengthFromFirstByte(c1);
            int c2, c3;
            switch (len)
            {
                default:
                    throw new IonException($"Invalid first-byte utf8 {c1}");
                case 1:
                    return c1;
                case 2:
                    c2 = ReadChar();
                    return Characters.Utf8TwoByteScalar(c1, c2);
                case 3:
                    c2 = ReadChar();
                    c3 = ReadChar();
                    return Characters.Utf8ThreeByteScalar(c1, c2, c3);
                case 4:
                    c2 = ReadChar();
                    c3 = ReadChar();
                    var c4 = ReadChar();
                    return Characters.Utf8FourByteScalar(c1, c2, c3, c4);
            }
        }

        /// <summary>
        /// Load the double-quoted string into the string builder
        /// </summary>
        /// <returns>The ending double quoted character</returns>
        /// <remarks>The stream has to be positioned at the opening double-quote</remarks>
        public int LoadDoubleQuotedString(StringBuilder sb, bool isClob)
        {
            while (true)
            {
                var c = ReadStringChar(Characters.ProhibitionContext.ShortChar);
                switch (c)
                {
                    case CharacterSequence.CharSeqEscapedNewlineSequence1:
                    case CharacterSequence.CharSeqEscapedNewlineSequence2:
                    case CharacterSequence.CharSeqEscapedNewlineSequence3:
                        continue;
                    case -1:
                    case '"':
                        FinishNextToken(TextConstants.TokenStringDoubleQuote, false);
                        return c;
                    case CharacterSequence.CharSeqNewlineSequence1:
                    case CharacterSequence.CharSeqNewlineSequence2:
                    case CharacterSequence.CharSeqNewlineSequence3:
                        throw new InvalidTokenException("Invalid new line in string");
                    case '\\':
                        c = ReadEscapedChar(c, isClob);
                        break;
                    default:
                        if (!isClob && !Characters.Is7BitChar(c))
                        {
                            c = ReadLargeCharSequence(c);
                        }

                        break;
                }

                if (!isClob && Characters.NeedsSurrogateEncoding(c))
                {
                    sb.Append(Characters.GetHighSurrogate(c));
                    c = Characters.GetLowSurrogate(c);
                }

                sb.Append((char) c);
            }
        }

        /// <summary>
        /// peeks into the input stream to see if the next token would be a double colon.  If indeed this is the case
        /// it skips the two colons and returns true.  If not it unreads the 1 or 2 real characters it read and return false.
        /// </summary>
        /// <returns>True if a double-colon token is skipped</returns>
        /// <remarks>It always consumes any preceding whitespace.</remarks>
        public bool TrySkipDoubleColon()
        {
            var c = SkipOverWhiteSpace(CommentStrategy.Ignore);
            if (c != ':') {
                UnreadChar(c);
                return false;
            }
            c = ReadChar();
            if (c == ':') 
                return true;
            
            UnreadChar(c);
            UnreadChar(':');
            return false;
        }

        /// <summary>
        /// Skip the double-quoted string into the string builder
        /// </summary>
        /// <remarks>The stream has to be positioned at the opening double-quote</remarks>
        private void SkipDoubleQuotedString()
        {
            while (true)
            {
                var c = ReadStringChar(Characters.ProhibitionContext.None);
                switch (c)
                {
                    case -1:
                        throw new UnexpectedEofException();
                    case CharacterSequence.CharSeqEscapedNewlineSequence1:
                    case CharacterSequence.CharSeqEscapedNewlineSequence2:
                    case CharacterSequence.CharSeqEscapedNewlineSequence3:
                        // new line normalization and counting is handled in ReadStringChar
                        throw new InvalidTokenException(c);
                    case '"':
                        return;
                    case '\\':
                        //TODO do we care???
                        ReadChar();
                        break;
                }
            }
        }

        /// <summary>
        /// Read an escaped character that starts with '\\' 
        /// </summary>
        /// <remarks>
        /// Since '\\' could be the mark of a new line we will skip that
        /// </remarks>
        private int ReadEscapedChar(int c, bool clob)
        {
            while (true)
            {
                switch (c)
                {
                    case CharacterSequence.CharSeqEscapedNewlineSequence1:
                    case CharacterSequence.CharSeqEscapedNewlineSequence2:
                    case CharacterSequence.CharSeqEscapedNewlineSequence3:
                        // loop again, we don't want empty escape chars
                        c = ReadStringChar(Characters.ProhibitionContext.None);
                        continue;
                    case '\\':
                        c = ReadChar();
                        if (c < 0)
                            throw new UnexpectedEofException();
                        c = ReadEscapedCharContent(c, clob);
                        if (c == CharacterSequence.CharSeqEscapedNewlineSequence1
                            || c == CharacterSequence.CharSeqEscapedNewlineSequence2
                            || c == CharacterSequence.CharSeqEscapedNewlineSequence3)
                        {
                            // loop again, we don't want empty escape chars
                            c = ReadStringChar(Characters.ProhibitionContext.None);
                            continue;
                        }

                        if (c == TextConstants.EscapeNotDefined)
                            throw new InvalidTokenException(c);
                        break;
                    default:
                        if (!clob && Characters.Is7BitChar(c))
                        {
                            c = ReadLargeCharSequence(c);
                        }

                        break;
                }

                // at this point we have a post-escaped character to return to the caller
                break;
            }

            if (c == CharacterSequence.CharSeqEof)
                return c;
            //TODO 8-bit value in clob?
            return c;
        }

        /// <summary>
        /// Replace the escaped character with the actual content
        /// </summary>
        private int ReadEscapedCharContent(int c1, bool clob)
        {
            if (c1 < 0)
            {
                //if '\' is followed by a newline sequence we can just return in to ReadEscapedChar to be skipped
                switch (c1)
                {
                    default:
                        throw new InvalidTokenException(c1);
                    case CharacterSequence.CharSeqEscapedNewlineSequence1:
                    case CharacterSequence.CharSeqEscapedNewlineSequence2:
                    case CharacterSequence.CharSeqEscapedNewlineSequence3:
                        return c1;
                }
            }

            if (!TextConstants.IsValidEscapeStart(c1))
                throw new InvalidTokenException(c1);

            var c2 = TextConstants.GetEscapeReplacementCharacter(c1);
            switch (c2)
            {
                case TextConstants.EscapeNotDefined:
                    throw new InvalidTokenException(c2);
                case TextConstants.EscapeLittleU:
                    if (clob)
                        throw new InvalidTokenException(c1);
                    c2 = ReadHexEscapeSequence(4);
                    break;
                case TextConstants.EscapeBigU:
                    if (clob)
                        throw new InvalidTokenException(c1);
                    c2 = ReadHexEscapeSequence(8);
                    break;
                case TextConstants.EscapeHex:
                    c2 = ReadHexEscapeSequence(2);
                    break;
            }

            return c2;
        }

        private int ReadHexEscapeSequence(int len)
        {
            var hexchar = 0;
            while (len > 0)
            {
                len--;
                var c = ReadChar();
                if (c < 0)
                    throw new UnexpectedEofException();

                var d = TextConstants.HexDigitValue(c);
                if (d < 0)
                    return -1;
                hexchar = (hexchar << 4) + d;
            }

            if (len > 0)
                throw new IonException($"Hex digit len exceeded maximum");

            return hexchar;
        }

        /// <summary>
        /// This will read the character unit in the context of a string, and will absorb escaped sequence
        /// </summary>
        /// <returns>Read char</returns>
        /// <remarks>This will NOT do encoding</remarks>
        private int ReadStringChar(Characters.ProhibitionContext prohibitionContext)
        {
            var c = _input.Read();
            if (Characters.IsProhibited(c, prohibitionContext))
                throw new IonException($"Invalid character {(char) c}");

            if (c == '\\' || c == '\n' || c == '\r')
            {
                c = EatNewLineSequence(c);
            }

            return c;
        }

        /// <summary>
        /// Depending on what the character is, read the new line sequence
        /// </summary>
        /// <param name="c">Starting char</param>
        /// <returns>The character code</returns>
        private int EatNewLineSequence(int c)
        {
            int c2;
            switch (c)
            {
                default:
                    throw new InvalidOperationException($"Nothing to absorb after {(char) c}");
                case '\\':
                    c2 = _input.Read();
                    switch (c2)
                    {
                        case '\r':
                            // DOS <cr><lf> (\r\n)  or old Mac <cr> 
                            var c3 = _input.Read();
                            if (c3 == '\n')
                            {
                                // DOS <cr><lf> (\r\n)
                                return CharacterSequence.CharSeqEscapedNewlineSequence3;
                            }

                            //old Mac <cr> , fall back
                            UnreadChar(c3);
                            return CharacterSequence.CharSeqEscapedNewlineSequence2;
                        case '\n':
                            //Unix line feed
                            return CharacterSequence.CharSeqEscapedNewlineSequence1;
                        default:
                            //not a slash new line, so we'll just return the slash 
                            // leave it to be handled elsewhere
                            UnreadChar(c2);
                            return c;
                    }
                case '\r':
                    c2 = _input.Read();
                    if (c2 == '\n')
                    {
                        // DOS <cr><lf> (\r\n)
                        return CharacterSequence.CharSeqNewlineSequence3;
                    }

                    //old Mac <cr> , fall back
                    UnreadChar(c2);
                    return CharacterSequence.CharSeqNewlineSequence2;
                case '\n':
                    return CharacterSequence.CharSeqNewlineSequence1;
            }
        }

        private int ReadChar()
        {
            var c = _input.Read();
            if (c == '\n' || c == '\r')
            {
                c = EatNewLineSequence(c);
            }

            return c;
        }

        /// <summary>
        /// This will unread the current char and depending on that might unread several more char that belongs
        /// to the same sequence
        /// </summary>
        /// <param name="c">Char to unread</param>
        private void UnreadChar(int c)
        {
            if (c >= 0)
            {
                _input.Unread(c);
                return;
            }

            switch (c)
            {
                default:
                    Debug.Assert(false, $"Invalid character encountered: {c}");
                    break;
                case CharacterSequence.CharSeqNewlineSequence1:
                    _input.Unread('\n');
                    break;
                case CharacterSequence.CharSeqNewlineSequence2:
                    _input.Unread('\r');
                    break;
                case CharacterSequence.CharSeqNewlineSequence3:
                    _input.Unread('\n');
                    _input.Unread('\r');
                    break;
                case CharacterSequence.CharSeqEscapedNewlineSequence1:
                    _input.Unread('\n');
                    _input.Unread('\\');
                    break;
                case CharacterSequence.CharSeqEscapedNewlineSequence2:
                    _input.Unread('\r');
                    _input.Unread('\\');
                    break;
                case CharacterSequence.CharSeqEscapedNewlineSequence3:
                    _input.Unread('\n');
                    _input.Unread('\r');
                    _input.Unread('\\');
                    break;
                case CharacterSequence.CharSeqEof:
                    //TODO can you 'unread' an EOF?
                    _input.Unread(CharacterSequence.CharSeqEof);
                    break;
            }
        }

        /// <summary>
        /// Finish the current 'token', skip to end if neccessary.
        /// </summary>
        public void FinishToken()
        {
            if (!UnfinishedToken) return;

            var c = SkipToEnd();
            UnreadChar(c);
            UnfinishedToken = false;
        }

        public void MarkTokenFinished()
        {
            UnfinishedToken = false;
            _base64PrefetchCount = 0;
        }

        public IonType LoadNumber(StringBuilder valueBuffer)
        {
            var c = ReadChar();
            var hasSign = c == '-' || c == '+';
            if (hasSign)
            {
                // if there is a sign character, we just consume it
                // here and get whatever is next in line
                valueBuffer.Append((char) c);
                c = ReadChar();
            }

            if (!char.IsDigit((char) c))
            {
                // if it's not a digit, this isn't a number the only non-digit it could have been was a
                // sign character, and we'll have read past that by now
                throw new InvalidTokenException(c);
            }

            var startWithZero = c == '0';
            if (startWithZero)
            {
                var c2 = ReadChar();
                if (Radix.Hex.IsPrefix(c2))
                {
                    valueBuffer.Append((char) c);
                    c = LoadRadixValue(valueBuffer, c2, Radix.Hex);
                    return FinishLoadNumber(valueBuffer, c, TextConstants.TokenHex);
                }

                if (Radix.Binary.IsPrefix(c2))
                {
                    valueBuffer.Append((char) c);
                    c = LoadRadixValue(valueBuffer, c2, Radix.Hex);
                    return FinishLoadNumber(valueBuffer, c, TextConstants.TokenHex);
                }

                UnreadChar(c2);
            }

            c = LoadDigits(valueBuffer, c);
            if (c == '-' || c == 'T')
            {
                // this better be a timestamp and it starts with a 4 digit
                // year followed by a dash and no leading sign
                if (hasSign)
                {
                    throw new IonException($"Numeric value followed by invalid character: {valueBuffer}{(char) c}");
                }

                var len = valueBuffer.Length;
                if (len != 4)
                {
                    throw new IonException($"Numeric value followed by invalid character: {valueBuffer}{(char) c}");
                }

                return LoadTimestamp(valueBuffer, c);
            }

            if (startWithZero)
            {
                // Ion doesn't allow leading zeros, so make sure our buffer only
                // has one character.
                var len = valueBuffer.Length;
                if (hasSign)
                {
                    len--; // we don't count the sign
                }

                if (len != 1)
                    throw new IonException("Invalid leading zero in number: " + valueBuffer);
            }

            int t;
            if (c == '.')
            {
                // so if it's a float of some sort
                // mark it as at least a DECIMAL
                // and read the "fraction" digits
                valueBuffer.Append((char) c);
                c = ReadChar();
                c = LoadDigits(valueBuffer, c);
                t = TextConstants.TokenDecimal;
            }
            else
            {
                t = TextConstants.TokenInt;
            }

            // see if we have an exponential as in 2d+3
            if (c == 'e' || c == 'E')
            {
                t = TextConstants.TokenFloat;
                valueBuffer.Append((char) c);
                c = LoadExponent(valueBuffer); // the unused lookahead char
            }
            else if (c == 'd' || c == 'D')
            {
                t = TextConstants.TokenDecimal;
                valueBuffer.Append((char) c);
                c = LoadExponent(valueBuffer);
            }

            return FinishLoadNumber(valueBuffer, c, t);
        }

        private IonType LoadTimestamp(StringBuilder valueBuffer, int i)
        {
            throw new NotImplementedException();
        }

        private int LoadExponent(StringBuilder valueBuffer)
        {
            var c = ReadChar();
            if (c == '-' || c == '+')
            {
                valueBuffer.Append((char) c);
                c = ReadChar();
            }

            c = LoadDigits(valueBuffer, c);

            if (c != '.') return c;

            valueBuffer.Append((char) c);
            c = ReadChar();
            c = LoadDigits(valueBuffer, c);

            return c;
        }

        private int LoadDigits(StringBuilder sb, int c)
        {
            if (!char.IsDigit((char) c))
                return c;

            sb.Append((char) c);

            return ReadNumeric(sb, Radix.Decimal, NumericState.Digit);
        }

        /// <summary>
        /// <see cref="LoadNumber"/> will always read 1 extra terminating character, so we unread it here
        /// </summary>
        /// <exception cref="InvalidTokenException">If the last character read is not a terminating char</exception>
        private IonType FinishLoadNumber(StringBuilder numericText, int c, int token)
        {
            // all forms of numeric need to stop someplace rational
            if (!IsTerminatingCharacter(c))
                throw new InvalidTokenException($"Numeric value followed by invalid character: {numericText}{(char) c}");

            // we read off the end of the number, so put back
            // what we don't want, but what ever we have is an int
            UnreadChar(c);
            return TextConstants.GetIonTypeOfToken(token);
        }

        private int LoadRadixValue(StringBuilder sb, int c2, Radix radix)
        {
            sb.Append((char) c2);

            return ReadNumeric(sb, radix);
        }

        private int ReadNumeric(StringBuilder buffer, Radix radix) => ReadNumeric(buffer, radix, NumericState.Start);

        private int ReadNumeric(StringBuilder buffer, Radix radix, NumericState state)
        {
            while (true)
            {
                var c = ReadChar();
                switch (state)
                {
                    case NumericState.Start:
                        if (radix.IsValidDigit(c))
                        {
                            buffer.Append(radix.NormalizeDigit((char) c));
                            state = NumericState.Digit;
                        }
                        else
                            return c;

                        break;
                    case NumericState.Digit:
                        if (radix.IsValidDigit(c))
                        {
                            buffer.Append(radix.NormalizeDigit((char) c));
                            state = NumericState.Digit;
                        }
                        else if (c == '_')
                        {
                            state = NumericState.Underscore;
                        }
                        else
                            return c;

                        break;
                    case NumericState.Underscore:
                        if (radix.IsValidDigit(c))
                        {
                            buffer.Append(radix.NormalizeDigit((char) c));
                            state = NumericState.Digit;
                        }
                        else
                        {
                            UnreadChar(c);
                            return '_';
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(state), state, null);
                }
            }
        }

        public void LoadSymbolIdentifier(StringBuilder valueBuffer)
        {
            var c = ReadChar();
            while (TextConstants.IsValidSymbolCharacter(c))
            {
                valueBuffer.Append((char) c);
                c = ReadChar();
            }

            UnreadChar(c);
        }

        public void LoadSymbolOperator(object sb)
        {
            throw new NotImplementedException();
        }

        public int LoadSingleQuotedString(StringBuilder valueBuffer, bool clobCharsOnly)
        {
            throw new NotImplementedException();
        }

        public int LoadTripleQuotedString(StringBuilder valueBuffer, bool clobCharsOnly)
        {
            throw new NotImplementedException();
        }
    }
}
