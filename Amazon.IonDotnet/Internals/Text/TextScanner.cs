/*
 * Copyright 2020 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

namespace Amazon.IonDotnet.Internals.Text
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Text;
    using Amazon.IonDotnet.Utils;

    /// <summary>
    /// This class is responsible for the main parsing/reading logic, which works directly with the
    /// <see cref="TextStream"/> abstraction to scan the input for interesting token as well as provide
    /// the method for reading the value.
    /// </summary>
    /// <remarks>
    /// At any point during the parsing, there is one active token type that represent the current state
    /// of the input. The raw text reader class can based on that token to read/load value, or to perform
    /// actions such as skipping to next token.
    /// </remarks>
    internal sealed class TextScanner
    {
        private readonly TextStream input;

        public TextScanner(TextStream input)
        {
            this.input = input;
        }

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
            Break,
        }

        public int Token { get; private set; } = -1;

        public bool UnfinishedToken { get; private set; }

        public int NextToken()
        {
            int token;
            var c = this.UnfinishedToken ? this.SkipToEnd() : this.SkipOverWhiteSpace(CommentStrategy.Ignore);

            this.UnfinishedToken = true;

            // get some of the common character out of the way to avoid long switch
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
            {
                this.UnreadChar(c);
                return this.FinishNextToken(TextConstants.TokenSymbolIdentifier, true);
            }

            if (c >= '0' && c <= '9')
            {
                token = this.ScanForNumericType(c);
                this.UnreadChar(c);
                return this.FinishNextToken(token, true);
            }

            switch (c)
            {
                default:
                    throw new InvalidTokenException(c);
                case -1:
                    return this.FinishNextToken(TextConstants.TokenEof, true);
                case '/':
                    this.UnreadChar(c);
                    return this.FinishNextToken(TextConstants.TokenSymbolOperator, true);
                case ':':
                    var c2 = this.ReadChar();
                    if (c2 == ':')
                    {
                        return this.FinishNextToken(TextConstants.TokenDoubleColon, true);
                    }

                    if (c2 == '}')
                    {
                        throw new IonException("Unexpected }");
                    }

                    this.UnreadChar(c2);
                    return this.FinishNextToken(TextConstants.TokenColon, true);
                case '{':
                    c2 = this.ReadChar();
                    if (c2 == '{')
                    {
                        return this.FinishNextToken(TextConstants.TokenOpenDoubleBrace, true);
                    }

                    this.UnreadChar(c2);
                    return this.FinishNextToken(TextConstants.TokenOpenBrace, true);
                case '}':
                    // detection of double closing braces is done
                    // in the parser in the blob and clob handling
                    // state - it's otherwise ambiguous with closing
                    // two structs together. See tryForDoubleBrace() below
                    return this.FinishNextToken(TextConstants.TokenCloseBrace, false);
                case '[':
                    return this.FinishNextToken(TextConstants.TokenOpenSquare, true);
                case ']':
                    return this.FinishNextToken(TextConstants.TokenCloseSquare, false);
                case '(':
                    return this.FinishNextToken(TextConstants.TokenOpenParen, true);
                case ')':
                    return this.FinishNextToken(TextConstants.TokenCloseParen, false);
                case ',':
                    return this.FinishNextToken(TextConstants.TokenComma, false);
                case '.':
                    c2 = this.ReadChar();
                    this.UnreadChar(c2);
                    if (!TextConstants.IsValidExtendedSymbolCharacter(c2))
                    {
                        return this.FinishNextToken(TextConstants.TokenDot, false);
                    }

                    this.UnreadChar('.');
                    return this.FinishNextToken(TextConstants.TokenSymbolOperator, true);
                case '\'':
                    if (this.Is2SingleQuotes())
                    {
                        return this.FinishNextToken(TextConstants.TokenStringTripleQuote, true);
                    }

                    return this.FinishNextToken(TextConstants.TokenSymbolQuoted, true);
                case '+':
                    if (this.PeekInf(c))
                    {
                        return this.FinishNextToken(TextConstants.TokenFloatInf, false);
                    }

                    this.UnreadChar(c);
                    return this.FinishNextToken(TextConstants.TokenSymbolOperator, true);
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
                    this.UnreadChar(c);
                    return this.FinishNextToken(TextConstants.TokenSymbolOperator, true);
                case '"':
                    return this.FinishNextToken(TextConstants.TokenStringDoubleQuote, true);
                case '$':
                case '_':
                    this.UnreadChar(c);
                    return this.FinishNextToken(TextConstants.TokenSymbolIdentifier, true);
                case '-':
                    // see if we have a number or what might be an extended symbol
                    c2 = this.ReadChar();
                    this.UnreadChar(c2);
                    if (char.IsDigit((char)c2))
                    {
                        token = this.ScanForNegativeNumbericType(c);
                        this.UnreadChar(c);
                        return this.FinishNextToken(token, true);
                    }

                    // this will consume the inf if it succeeds
                    if (this.PeekInf(c))
                    {
                        return this.FinishNextToken(TextConstants.TokenFloatMinusInf, false);
                    }

                    this.UnreadChar(c);
                    return this.FinishNextToken(TextConstants.TokenSymbolOperator, true);
            }
        }

        public void LoadBlob(StringBuilder sb)
        {
            var c = this.SkipOverWhiteSpace(CommentStrategy.Break);
            while (true)
            {
                if (c == CharacterSequence.CharSeqEof)
                {
                    throw new UnexpectedEofException();
                }

                if (c == '}')
                {
                    break;
                }

                sb.Append((char)c);
                c = this.SkipOverWhiteSpace(CommentStrategy.Break);
            }

            c = this.ReadChar();
            if (c == CharacterSequence.CharSeqEof)
            {
                throw new UnexpectedEofException();
            }

            if (c != '}')
            {
                throw new IonException("Blob not closed properly");
            }

            // now we've seen }}, unread them so they can be skipped
            this.UnreadChar('}');
            this.UnreadChar('}');
        }

        public void SkipOverStruct() => this.SkipOverContainer('}');

        public void SkipOverSexp() => this.SkipOverContainer(')');

        public void SkipOverList() => this.SkipOverContainer(']');

        /// <summary>
        /// Skip the whitespace and comments to the next token.
        /// </summary>
        /// <returns>True if any whitespace is skipped.</returns>
        public bool SkipWhiteSpace() => this.SkipWhiteSpaceWithCommentStrategy(CommentStrategy.Ignore);

        /// <summary>
        /// Load the double-quoted string into the string builder.
        /// </summary>
        /// <param name="sb">StringBuilder to load into.</param>
        /// <param name="isClob">Bool value indicating if it's a clob.</param>
        /// <returns>The ending double quoted character.</returns>
        /// <remarks>The stream has to be positioned at the opening double-quote.</remarks>
        public int LoadDoubleQuotedString(StringBuilder sb, bool isClob)
        {
            while (true)
            {
                var c = this.ReadStringChar(Characters.ProhibitionContext.ShortChar);

                // CLOB texts should be only 7-bit ASCII characters
                if (isClob)
                {
                    this.Require7BitChar(c);
                }

                switch (c)
                {
                    case CharacterSequence.CharSeqEscapedNewlineSequence1:
                    case CharacterSequence.CharSeqEscapedNewlineSequence2:
                    case CharacterSequence.CharSeqEscapedNewlineSequence3:
                        continue;
                    case -1:
                        this.FinishNextToken(isClob ? this.Token : TextConstants.TokenStringDoubleQuote, isClob);
                        return this.Token == TextConstants.TokenStringDoubleQuote ? TextConstants.TokenEof : c;
                    case '"':
                        if (isClob)
                        {
                            this.HandleClobsWithDoubleQuote();
                        }

                        this.FinishNextToken(isClob ? this.Token : TextConstants.TokenStringDoubleQuote, isClob);
                        return c;
                    case CharacterSequence.CharSeqNewlineSequence1:
                    case CharacterSequence.CharSeqNewlineSequence2:
                    case CharacterSequence.CharSeqNewlineSequence3:
                        throw new InvalidTokenException("Invalid new line in string");
                    case '\\':
                        c = this.ReadEscapedChar(c, isClob);
                        break;
                }

                if (!isClob)
                {
                    if (char.IsHighSurrogate((char)c))
                    {
                        sb.Append((char)c);
                        c = this.ReadChar();
                        c = this.ReadEscapedChar(c, isClob);
                        if (!char.IsLowSurrogate((char)c))
                        {
                            throw new IonException($"Invalid character format {(char)c}");
                        }
                    }
                    else if (char.IsLowSurrogate((char)c))
                    {
                        throw new IonException($"Invalid character format {(char)c}");
                    }
                }

                sb.Append((char)c);
            }
        }

        /// <summary>
        /// Peeks into the input stream to see what non-whitespace character is coming up.
        /// </summary>
        /// <returns>The type of token next to '{{'.</returns>
        /// <remarks>This will unread whatever non-whitespace character it read.</remarks>
        public int PeekLobStartPunctuation()
        {
            int c = this.SkipOverWhiteSpace(CommentStrategy.Break);
            if (c == '"')
            {
                return TextConstants.TokenStringDoubleQuote;
            }

            if (c != '\'')
            {
                this.UnreadChar(c);
                return TextConstants.TokenError;
            }

            c = this.ReadChar();
            if (c != '\'')
            {
                this.UnreadChar(c);
                this.UnreadChar('\'');
                return TextConstants.TokenError;
            }

            c = this.ReadChar();
            if (c != '\'')
            {
                this.UnreadChar(c);
                this.UnreadChar('\'');
                this.UnreadChar('\'');
                return TextConstants.TokenError;
            }

            return TextConstants.TokenStringTripleQuote;
        }

        public int LoadSingleQuotedString(StringBuilder valueBuffer, bool clobCharsOnly)
        {
            while (true)
            {
                var c = this.ReadStringChar(Characters.ProhibitionContext.None);
                switch (c)
                {
                    case CharacterSequence.CharSeqEscapedNewlineSequence1:
                    case CharacterSequence.CharSeqEscapedNewlineSequence2:
                    case CharacterSequence.CharSeqEscapedNewlineSequence3:
                        continue;
                    case -1:
                    case '\'':
                        this.MarkTokenFinished();
                        return c;
                    case CharacterSequence.CharSeqNewlineSequence1:
                    case CharacterSequence.CharSeqNewlineSequence2:
                    case CharacterSequence.CharSeqNewlineSequence3:
                        throw new InvalidTokenException(c);
                    case '\\':
                        c = this.ReadChar();
                        c = this.ReadEscapedCharContent(c, clobCharsOnly);
                        break;
                }

                if (!clobCharsOnly)
                {
                    if (char.IsHighSurrogate((char)c))
                    {
                        valueBuffer.Append((char)c);
                        c = this.ReadChar();
                        if (!char.IsLowSurrogate((char)c))
                        {
                            throw new IonException($"Invalid character format {(char)c}");
                        }
                    }
                    else if (char.IsLowSurrogate((char)c))
                    {
                        throw new IonException($"Invalid character format {(char)c}");
                    }
                }
                else if (Characters.Is8BitChar(c))
                {
                    throw new InvalidTokenException(c);
                }

                valueBuffer.Append((char)c);
            }
        }

        /// <summary>
        /// peeks into the input stream to see if the next token would be a double colon.  If indeed this is the case
        /// it skips the two colons and returns true.  If not it unreads the 1 or 2 real characters it read and return false.
        /// </summary>
        /// <returns>True if a double-colon token is skipped.</returns>
        /// <remarks>It always consumes any preceding whitespace.</remarks>
        public bool TrySkipDoubleColon()
        {
            var c = this.SkipOverWhiteSpace(CommentStrategy.Ignore);
            if (c != ':')
            {
                this.UnreadChar(c);
                return false;
            }

            c = this.ReadChar();
            if (c == ':')
            {
                return true;
            }

            this.UnreadChar(c);
            this.UnreadChar(':');
            return false;
        }

        /// <summary>
        /// Finish the current 'token', skip to end if neccessary.
        /// </summary>
        public void FinishToken()
        {
            if (!this.UnfinishedToken)
            {
                return;
            }

            var c = this.SkipToEnd();
            this.UnreadChar(c);
            this.UnfinishedToken = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MarkTokenFinished()
        {
            this.UnfinishedToken = false;
        }

        public IonType LoadNumber(StringBuilder valueBuffer)
        {
            var c = this.ReadChar();
            var hasSign = c == '-' || c == '+';
            if (hasSign)
            {
                // if there is a sign character, we just consume it
                // here and get whatever is next in line
                valueBuffer.Append((char)c);
                c = this.ReadChar();
            }

            if (!char.IsDigit((char)c))
            {
                // if it's not a digit, this isn't a number the only non-digit it could have been was a
                // sign character, and we'll have read past that by now
                throw new InvalidTokenException(c);
            }

            var startWithZero = c == '0';
            if (startWithZero)
            {
                var c2 = this.ReadChar();
                if (Radix.Hex.IsPrefix(c2))
                {
                    valueBuffer.Append((char)c);
                    c = this.LoadRadixValue(valueBuffer, c2, Radix.Hex);
                    return this.FinishLoadNumber(valueBuffer, c, TextConstants.TokenHex);
                }

                if (Radix.Binary.IsPrefix(c2))
                {
                    valueBuffer.Append((char)c);
                    c = this.LoadRadixValue(valueBuffer, c2, Radix.Hex);
                    return this.FinishLoadNumber(valueBuffer, c, TextConstants.TokenHex);
                }

                this.UnreadChar(c2);
            }

            c = this.LoadDigits(valueBuffer, c);
            if (c == '-' || c == 'T')
            {
                // this better be a timestamp and it starts with a 4 digit
                // year followed by a dash and no leading sign
                if (hasSign)
                {
                    throw new FormatException($"Numeric value followed by invalid character: {valueBuffer}{(char)c}");
                }

                var len = valueBuffer.Length;
                if (len != 4)
                {
                    throw new FormatException($"Numeric value followed by invalid character: {valueBuffer}{(char)c}");
                }

                return this.LoadTimestamp(valueBuffer, c);
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
                {
                    throw new FormatException("Invalid leading zero in number: " + valueBuffer);
                }
            }

            int t;
            if (c == '.')
            {
                // so if it's a floating point number
                // mark it as decimal by default, then continue to read the rest
                valueBuffer.Append((char)c);
                c = this.ReadChar();
                c = this.LoadDigits(valueBuffer, c);
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
                valueBuffer.Append((char)c);
                c = this.LoadExponent(valueBuffer); // the unused lookahead char
            }
            else if (c == 'd' || c == 'D')
            {
                t = TextConstants.TokenDecimal;
                valueBuffer.Append((char)c);
                c = this.LoadExponent(valueBuffer);
            }

            return this.FinishLoadNumber(valueBuffer, c, t);
        }

        public void LoadSymbolIdentifier(StringBuilder valueBuffer)
        {
            var c = this.ReadChar();
            while (TextConstants.IsValidSymbolCharacter(c))
            {
                valueBuffer.Append((char)c);
                c = this.ReadChar();
            }

            this.UnreadChar(c);
        }

        public void LoadSymbolOperator(StringBuilder sb)
        {
            var c = this.ReadChar();

            // look ahead for +inf and -inf, this will consume the inf if it succeeds
            if ((c == '+' || c == '-') && this.PeekInf(c))
            {
                sb.Append((char)c);
                sb.Append("inf");
                return;
            }

            while (TextConstants.IsValidExtendedSymbolCharacter(c))
            {
                sb.Append((char)c);
                c = this.ReadChar();
            }

            this.UnreadChar(c);
        }

        public int LoadTripleQuotedString(StringBuilder sb, bool isClob)
        {
            while (true)
            {
                var c = this.ReadTripleQuotedChar(isClob);
                switch (c)
                {
                    case CharacterSequence.CharSeqStringTerminator:
                        this.FinishNextToken(isClob ? this.Token : TextConstants.TokenStringTripleQuote, isClob);
                        return c;
                    case CharacterSequence.CharSeqEof:
                        this.FinishNextToken(isClob ? this.Token : TextConstants.TokenStringTripleQuote, isClob);
                        return this.Token == TextConstants.TokenStringTripleQuote ? TextConstants.TokenEof : c;

                    // new line normalization and counting is handled in read_char
                    case CharacterSequence.CharSeqNewlineSequence1:
                        c = '\n';
                        break;
                    case CharacterSequence.CharSeqNewlineSequence2:
                        c = '\n';
                        break;
                    case CharacterSequence.CharSeqNewlineSequence3:
                        c = '\n';
                        break;
                    case CharacterSequence.CharSeqEscapedNewlineSequence1:
                    case CharacterSequence.CharSeqEscapedNewlineSequence2:
                    case CharacterSequence.CharSeqEscapedNewlineSequence3:
                    case CharacterSequence.CharSeqStringNonTerminator:
                        continue;
                }

                // if this isn't a clob we need to decode UTF8 and
                // handle surrogate encoding (otherwise we don't care)
                if (!isClob)
                {
                    if (char.IsHighSurrogate((char)c))
                    {
                        sb.Append((char)c);
                        c = this.ReadChar();
                        if (!char.IsLowSurrogate((char)c))
                        {
                            throw new IonException($"Invalid character format {(char)c}");
                        }
                    }
                    else if (char.IsLowSurrogate((char)c))
                    {
                        throw new IonException($"Invalid character format {(char)c}");
                    }
                }
                else
                {
                    // CLOB texts should be only 7-bit ASCII characters
                    this.Require7BitChar(c);
                }

                sb.Append((char)c);
            }
        }

        public int PeekNullTypeSymbol()
        {
            // the '.' has to follow the 'null' immediately
            var c = this.ReadChar();
            if (c != '.')
            {
                this.UnreadChar(c);
                return TextConstants.KeywordNone;
            }

            // we have a dot, start reading through the following non-whitespace
            // and we'll collect it so that we can unread it in the event
            // we don't actually see a type name
            Span<int> readAhead = stackalloc int[TextConstants.TnMaxNameLength + 1];
            var readCount = 0;

            while (readCount < TextConstants.TnMaxNameLength + 1)
            {
                c = this.ReadChar();
                readAhead[readCount++] = c;
                if (!char.IsLetter((char)c))
                {
                    // it's not a letter we care about but it is
                    // a valid end of const, so maybe we have a keyword now
                    // we always exit the loop here since we look
                    // too far so any letter is invalid at pos 10
                    break;
                }
            }

            // now lets get the keyword value from our bit mask
            // at this point we can fail since we may have hit
            // a valid terminator before we're done with all key
            // words.  We even have to check the length.
            // for example "in)" matches both letters to the
            // typename int and terminates validly - but isn't
            // long enough, but with length we have enough to be sure
            // with the actual type names we're using in 1.0
            var kw = TextConstants.TypeNameKeyWordFromMask(readAhead, readCount - 1);
            if (kw == TextConstants.KeywordUnrecognized)
            {
                var sb = new StringBuilder();
                for (var i = 0; i < readCount; i++)
                {
                    sb.Append((char)readAhead[i]);
                }

                throw new IonException($"invalid type name on a typed null value: {sb}");
            }

            // since we're accepting the rest we aren't unreading anything
            // else - but we still have to unread the character that stopped us
            this.UnreadChar(c);

            return kw;
        }

        /// <summary>
        /// Variant of <see cref="ScanForNumericType"/> where the passed in start
        /// character is '-'.
        /// </summary>
        /// <param name="c">First character, should be '-'.</param>
        /// <returns>Numeric token type.</returns>
        /// <exception cref="InvalidTokenException">When an illegal token is encountered.</exception>
        /// <remarks>This will unread the minus sign.</remarks>
        private int ScanForNegativeNumbericType(int c)
        {
            Debug.Assert(c == '-', "c is not '-'");
            c = this.ReadChar();
            var t = this.ScanForNumericType(c);
            if (t == TextConstants.TokenTimestamp)
            {
                throw new InvalidTokenException(c);
            }

            // unread the '-'
            this.UnreadChar(c);
            return t;
        }

        /// <summary>
        /// We encountered a numeric character (digit or minus), now we scan a little
        /// way ahead to spot some of the numeric types.
        /// <para>
        /// This only looks far enough (2-6 chars) to identify hex and timestamp,
        /// it might encounter chars like 'd' or 'e' and decide if this token is float
        /// or decimal (or int), but it might return TOKEN_UNKNOWN_NUMERIC.
        /// </para>
        /// </summary>
        /// <param name="c1">First numeric char.</param>
        /// <returns>Numeric token type.</returns>
        /// <remarks>It will unread everything it reads, and the character passed in as the first digit.</remarks>
        private int ScanForNumericType(int c1)
        {
            var t = TextConstants.TokenUnknownNumeric;
            Span<int> readChars = stackalloc int[6];
            var readCharCount = 0;
            Debug.Assert(char.IsDigit((char)c1), "c1 IsDigit is false");

            var c = this.ReadChar();
            readChars[readCharCount++] = c;
            if (c1 == '0')
            {
                // check for hex
                switch (c)
                {
                    default:
                        if (this.IsTerminatingCharacter(c))
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
                if (char.IsDigit((char)c))
                {
                    // 2nd digit
                    // it might be a timestamp if we have 4 digits, a dash,
                    // and a digit
                    c = this.ReadChar();
                    readChars[readCharCount++] = c;
                    if (char.IsDigit((char)c))
                    {
                        // digit 3
                        c = this.ReadChar();
                        readChars[readCharCount++] = c;
                        if (char.IsDigit((char)c))
                        {
                            // digit 4, year
                            c = this.ReadChar();
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
                this.UnreadChar(c);
            }
            while (readCharCount > 0);

            return t;
        }

        private bool IsTerminatingCharacter(int c)
        {
            switch (c)
            {
                default:
                    return TextConstants.IsNumericStop(c);
                case '/':
                    c = this.ReadChar();
                    this.UnreadChar(c);
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
        /// Ion-text allows +inf and -inf.
        /// </summary>
        private bool PeekInf(int c)
        {
            if (c != '+' && c != '-')
            {
                return false;
            }

            c = this.ReadChar();
            if (c == 'i')
            {
                c = this.ReadChar();
                if (c == 'n')
                {
                    c = this.ReadChar();
                    if (c == 'f')
                    {
                        c = this.ReadChar();
                        if (this.IsTerminatingCharacter(c))
                        {
                            this.UnreadChar(c);
                            return true;
                        }

                        this.UnreadChar(c);
                        c = 'f';
                    }

                    this.UnreadChar(c);
                    c = 'n';
                }

                this.UnreadChar(c);
                c = 'i';
            }

            this.UnreadChar(c);
            return false;
        }

        /// <summary>
        /// This peeks ahead to see if the next two characters are single quotes.
        /// This would finish off a triple quote when the first quote has been read.
        /// </summary>
        /// <returns>True if the next two characters are single quotes.</returns>
        /// <remarks>
        /// If it succeeds it will consume the 2 quotes.
        /// If it fails it will unread.
        /// </remarks>
        private bool Is2SingleQuotes()
        {
            var c = this.ReadChar();
            if (c != '\'')
            {
                this.UnreadChar(c);
                return false;
            }

            c = this.ReadChar();
            if (c == '\'')
            {
                return true;
            }

            this.UnreadChar(c);
            this.UnreadChar('\'');
            return false;
        }

        private int FinishNextToken(int token, bool contentIsWaiting)
        {
            this.Token = token;
            this.UnfinishedToken = contentIsWaiting;
            return token;
        }

        /// <summary>
        /// Skip to the end of a token block.
        /// </summary>
        /// <returns>New token.</returns>
        /// <exception cref="InvalidTokenException">When the token read is unknown.</exception>
        private int SkipToEnd()
        {
            int c;
            switch (this.Token)
            {
                case TextConstants.TokenUnknownNumeric:
                    c = this.SkipOverNumber();
                    break;
                case TextConstants.TokenInt:
                    c = this.SkipOverInt();
                    break;
                case TextConstants.TokenHex:
                    c = this.SkipOverRadix(Radix.Hex);
                    break;
                case TextConstants.TokenBinary:
                    c = this.SkipOverRadix(Radix.Binary);
                    break;
                case TextConstants.TokenDecimal:
                    c = this.SkipOverDecimal();
                    break;
                case TextConstants.TokenFloat:
                    c = this.SkipOverFloat();
                    break;
                case TextConstants.TokenTimestamp:
                    c = this.SkipOverTimestamp();
                    break;
                case TextConstants.TokenSymbolIdentifier:
                    c = this.SkipOverSymbolIdentifier();
                    break;
                case TextConstants.TokenSymbolQuoted:
                    Debug.Assert(!this.Is2SingleQuotes(), "Is2SingleQuotes is true");
                    c = this.SkipSingleQuotedString();
                    break;
                case TextConstants.TokenSymbolOperator:
                    c = this.SkipOverSymbolOperator();
                    break;
                case TextConstants.TokenStringDoubleQuote:
                    this.SkipDoubleQuotedString();
                    c = this.SkipOverWhiteSpace(CommentStrategy.Ignore);
                    break;
                case TextConstants.TokenStringTripleQuote:
                    this.SkipTripleQuotedString(CommentStrategy.Ignore);
                    c = this.SkipOverWhiteSpace(CommentStrategy.Ignore);
                    break;
                case TextConstants.TokenOpenDoubleBrace:
                    // works just like a pair of nested structs
                    // since "skip_over" doesn't care about formal
                    // syntax (like requiring field names);
                    this.SkipOverBlob();
                    c = this.ReadChar();
                    break;
                case TextConstants.TokenOpenBrace:
                    this.SkipOverStruct();
                    c = this.ReadChar();
                    break;
                case TextConstants.TokenOpenParen:
                    this.SkipOverSexp(); // you can't save point a scanned sexp (right now anyway)
                    c = this.ReadChar();
                    break;
                case TextConstants.TokenOpenSquare:
                    this.SkipOverList(); // you can't save point a scanned list (right now anyway)
                    c = this.ReadChar();
                    break;
                default:
                    // Unknown token
                    throw new InvalidTokenException(this.Token);
            }

            if (TextConstants.IsWhiteSpace(c))
            {
                c = this.SkipOverWhiteSpace(CommentStrategy.Ignore);
            }

            this.UnfinishedToken = false;
            return c;
        }

        private void SkipOverBlob()
        {
            // skip over whitespace, but not the '/' character, since it is a valid base64 char
            var c = this.SkipOverWhiteSpace(CommentStrategy.Break);
            while (true)
            {
                if (c == CharacterSequence.CharSeqEof)
                {
                    throw new UnexpectedEofException();
                }

                if (c == '}')
                {
                    break;
                }

                c = this.SkipOverWhiteSpace(CommentStrategy.Break);
            }

            c = this.ReadChar();

            if (c == CharacterSequence.CharSeqEof)
            {
                throw new UnexpectedEofException();
            }

            if (c != '}')
            {
                throw new IonException("Blob not closed properly");
            }
        }

        private void SkipTripleQuotedString(CommentStrategy commentStrategy)
        {
            while (true)
            {
                var c = this.ReadChar();
                switch (c)
                {
                    case CharacterSequence.CharSeqEof:
                        throw new UnexpectedEofException();
                    case '\\':
                        var escaped = this.ReadChar();
                        if (escaped == CharacterSequence.CharSeqEof)
                        {
                            throw new UnexpectedEofException();
                        }

                        break;
                    case '\'':
                        c = this.ReadChar();
                        if (c == CharacterSequence.CharSeqEof)
                        {
                            throw new UnexpectedEofException();
                        }

                        // the 2nd '
                        if (c == '\'')
                        {
                            c = this.ReadChar();
                            if (c == CharacterSequence.CharSeqEof)
                            {
                                throw new UnexpectedEofException();
                            }

                            // the 3rd
                            var next = this.ReadChar();
                            if (next == CharacterSequence.CharSeqEof)
                            {
                                throw new UnexpectedEofException();
                            }

                            if (c == next)
                            {
                                c = this.SkipOverWhiteSpace(commentStrategy);
                                if (c == '\'' && this.Is2SingleQuotes())
                                {
                                    // the next segment is triple quoted, continue to skip
                                    break;
                                }

                                // otherwise unread that char and return
                                this.MarkTokenFinished();
                                this.UnreadChar(c);
                                return;
                            }
                        }

                        break;
                }
            }
        }

        private void SkipOverContainer(char terminator)
        {
            Debug.Assert(terminator == '}' || terminator == ']' || terminator == ')', "terminator is not '}', ']', or ')'");

            while (true)
            {
                var c = this.SkipOverWhiteSpace(CommentStrategy.Ignore);
                switch (c)
                {
                    case -1:
                        throw new UnexpectedEofException();
                    case '}':
                    case ']':
                    case ')':
                        if (c == terminator)
                        {
                            return;
                        }

                        break;
                    case '"':
                        this.SkipDoubleQuotedString();
                        break;
                    case '\'':
                        if (this.Is2SingleQuotes())
                        {
                            this.SkipTripleQuotedString(CommentStrategy.Ignore);
                        }
                        else
                        {
                            c = this.SkipSingleQuotedString();
                            this.UnreadChar(c);
                        }

                        break;
                    case '(':
                        this.SkipOverContainer(')');
                        break;
                    case '[':
                        this.SkipOverContainer(']');
                        break;
                    case '{':
                        // this consumes lobs as well since the double
                        // braces count correctly and the contents
                        // of either clobs or blobs will be just content
                        c = this.ReadChar();
                        if (c == '{')
                        {
                            // 2nd '{' - it's a lob of some sort - let's find out what sort
                            c = this.SkipOverWhiteSpace(CommentStrategy.Break);
                            int lobType;
                            if (c == '"')
                            {
                                // double-quoted clob
                                lobType = TextConstants.TokenStringDoubleQuote;
                            }
                            else if (c == '\'')
                            {
                                // triple-quoted clob or error
                                if (!this.Is2SingleQuotes())
                                {
                                    throw new InvalidTokenException(c);
                                }

                                lobType = TextConstants.TokenStringTripleQuote;
                            }
                            else
                            {
                                // blob
                                this.UnreadChar(c);
                                lobType = TextConstants.TokenOpenDoubleBrace;
                            }

                            this.SkipOverLob(lobType);
                        }
                        else if (c != '}')
                        {
                            // if c=='}' we just have an empty struct, ignore
                            this.UnreadChar(c);
                            this.SkipOverContainer('}');
                        }

                        break;
                }
            }
        }

        private void SkipOverLob(int lobType)
        {
            switch (lobType)
            {
                default:
                    throw new InvalidTokenException(lobType);
                case TextConstants.TokenStringDoubleQuote:
                    this.SkipDoubleQuotedString();
                    this.SkipClobClosePunctuation();
                    break;
                case TextConstants.TokenStringTripleQuote:
                    this.SkipTripleQuotedString(CommentStrategy.Error);
                    this.SkipClobClosePunctuation();
                    break;
                case TextConstants.TokenOpenDoubleBrace:
                    this.SkipOverBlob();
                    break;
            }
        }

        /// <summary>
        /// Expect optional whitespace(s) and }}.
        /// </summary>
        private void SkipClobClosePunctuation()
        {
            var c = this.SkipOverWhiteSpace(CommentStrategy.Error);
            if (c == '}')
            {
                c = this.ReadChar();
                if (c == '}')
                {
                    return;
                }

                this.UnreadChar(c);
                c = '}';
            }

            this.UnreadChar(c);
            throw new IonException("Invalid clob closing punctuation");
        }

        private int SkipOverSymbolOperator()
        {
            var c = this.ReadChar();
            if (this.PeekInf(c))
            {
                return this.ReadChar();
            }

            while (TextConstants.IsValidExtendedSymbolCharacter(c))
            {
                c = this.ReadChar();
            }

            return c;
        }

        private int SkipOverSymbolIdentifier()
        {
            var c = this.ReadChar();

            while (TextConstants.IsValidSymbolCharacter(c))
            {
                c = this.ReadChar();
            }

            return c;
        }

        private int SkipOverTimestamp()
        {
            throw new NotImplementedException();
        }

        private int SkipOverFloat() => this.SkipOverNumber();

        private int SkipOverDecimal() => this.SkipOverNumber();

        private int SkipOverRadix(Radix radix)
        {
            var c = this.ReadChar();
            if (c == '-')
            {
                c = this.ReadChar();
            }

            Debug.Assert(c == '0', "c is not equal to '0'");
            c = this.ReadChar();
            Debug.Assert(radix.IsPrefix(c), "IsPrefix(c) is false");

            this.SkipOverNumber();

            if (!this.IsTerminatingCharacter(c))
            {
                throw new InvalidTokenException(c);
            }

            return c;
        }

        private int SkipOverInt()
        {
            var c = this.ReadChar();
            if (c == '-')
            {
                c = this.ReadChar();
            }

            c = this.SkipOverDigits(c);
            if (!this.IsTerminatingCharacter(c))
            {
                throw new InvalidTokenException(c);
            }

            return c;
        }

        private int SkipOverNumber()
        {
            var c = this.ReadChar();

            // first consume any leading 0 to get it out of the way
            if (c == '-')
            {
                c = this.ReadChar();
            }

            // could be a long int, a decimal, a float
            // it cannot be a hex or a valid timestamp
            // so scan digits - if decimal can more digits
            // if d or e eat possible sign
            // scan to end of digits
            c = this.SkipOverDigits(c);
            if (c == '.')
            {
                c = this.ReadChar();
                c = this.SkipOverDigits(c);
            }

            if (c == 'd' || c == 'D' || c == 'e' || c == 'E')
            {
                c = this.ReadChar();
                if (c == '-' || c == '+')
                {
                    c = this.ReadChar();
                }

                c = this.SkipOverDigits(c);
            }

            if (!this.IsTerminatingCharacter(c))
            {
                throw new InvalidTokenException(c);
            }

            return c;
        }

        private int SkipOverDigits(int c)
        {
            while (char.IsDigit((char)c))
            {
                c = this.ReadChar();
            }

            return c;
        }

        private bool OnComment(CommentStrategy commentStrategy)
        {
            // A '/' character has been found, so break the loop as it may be a valid blob character.
            if (commentStrategy == CommentStrategy.Break)
            {
                return false;
            }

            int next;

            // Skip over all of the comment's text.
            if (commentStrategy == CommentStrategy.Ignore)
            {
                next = this.ReadChar();
                switch (next)
                {
                    default:
                        this.UnreadChar(next);
                        return false;
                    case '/':
                        // valid comment
                        this.SkipSingleLineComment();
                        return true;
                    case '*':
                        // valid block comment
                        this.SkipBlockComment();
                        return true;
                }
            }

            // here means CommentStrategy.Error
            // If it's a valid comment, throw an error.
            next = this.ReadChar();
            if (next == '/' || next == '*')
            {
                throw new InvalidTokenException("Illegal comment");
            }

            this.UnreadChar(next);
            return false;
        }

        /// <summary>
        /// Must be called right after "/*".
        /// </summary>
        private void SkipBlockComment()
        {
            while (true)
            {
                var c = this.ReadChar();
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
                            c = this.ReadChar();
                            if (c == '/')
                            {
                                return;
                            }

                            if (c != '*')
                            {
                                break;
                            }
                        }

                        break;
                }
            }
        }

        /// <summary>
        /// Must be called right after "//".
        /// </summary>
        private void SkipSingleLineComment()
        {
            while (true)
            {
                var c = this.ReadChar();
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

                // still in the comment, read another character
            }
        }

        /// <summary>
        /// Skip whitespace.
        /// </summary>
        /// <param name="commentStrategy">Comment strategy to apply.</param>
        /// <returns>Next char(token) in the stream.</returns>
        private int SkipOverWhiteSpace(CommentStrategy commentStrategy)
        {
            this.SkipWhiteSpaceWithCommentStrategy(commentStrategy);
            return this.ReadChar();
        }

        private bool SkipWhiteSpaceWithCommentStrategy(CommentStrategy commentStrategy)
        {
            var anyWhitespace = false;
            int c;
            while (true)
            {
                c = this.ReadChar();
                switch (c)
                {
                    default:
                        goto Done;
                    case ' ':
                    case '\t':
                    case '\v':
                    case '\f':
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
                        if (!this.OnComment(commentStrategy))
                        {
                            goto Done;
                        }

                        anyWhitespace = true;
                        break;
                }
            }

        Done:
            this.UnreadChar(c);
            return anyWhitespace;
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
                    c2 = this.ReadChar();
                    return Characters.Utf8TwoByteScalar(c1, c2);
                case 3:
                    c2 = this.ReadChar();
                    c3 = this.ReadChar();
                    return Characters.Utf8ThreeByteScalar(c1, c2, c3);
                case 4:
                    c2 = this.ReadChar();
                    c3 = this.ReadChar();
                    var c4 = this.ReadChar();
                    return Characters.Utf8FourByteScalar(c1, c2, c3, c4);
            }
        }

        private int SkipSingleQuotedString()
        {
            while (true)
            {
                var c = this.ReadStringChar(Characters.ProhibitionContext.None);
                switch (c)
                {
                    case CharacterSequence.CharSeqEof:
                        throw new UnexpectedEofException();
                    case '\'':
                        var next = this.ReadChar();
                        if (next == CharacterSequence.CharSeqEof)
                        {
                            throw new UnexpectedEofException();
                        }

                        return next;
                    case '\\':
                        var escaped = this.ReadChar();
                        if (escaped == CharacterSequence.CharSeqEof)
                        {
                            throw new UnexpectedEofException();
                        }

                        break;
                }
            }
        }

        /// <summary>
        /// Skip the double-quoted string into the string builder.
        /// </summary>
        /// <remarks>The stream has to be positioned at the opening double-quote.</remarks>
        private void SkipDoubleQuotedString()
        {
            while (true)
            {
                var c = this.ReadStringChar(Characters.ProhibitionContext.None);
                switch (c)
                {
                    case CharacterSequence.CharSeqEof:
                        throw new UnexpectedEofException();
                    case CharacterSequence.CharSeqEscapedNewlineSequence1:
                    case CharacterSequence.CharSeqEscapedNewlineSequence2:
                    case CharacterSequence.CharSeqEscapedNewlineSequence3:
                        // new line normalization and counting is handled in ReadStringChar
                        throw new InvalidTokenException(c);
                    case '"':
                        return;
                    case '\\':
                        var escaped = this.ReadChar();
                        if (escaped == CharacterSequence.CharSeqEof)
                        {
                            throw new UnexpectedEofException();
                        }

                        break;
                }
            }
        }

        /// <summary>
        /// Read an escaped character that starts with '\\'.
        /// </summary>
        /// <remarks>
        /// Since '\\' could be the mark of a new line we will skip that.
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
                        c = this.ReadStringChar(Characters.ProhibitionContext.None);
                        continue;
                    case '\\':
                        c = this.ReadChar();
                        if (c < 0)
                        {
                            throw new UnexpectedEofException();
                        }

                        c = this.ReadEscapedCharContent(c, clob);
                        if (c == CharacterSequence.CharSeqEscapedNewlineSequence1
                            || c == CharacterSequence.CharSeqEscapedNewlineSequence2
                            || c == CharacterSequence.CharSeqEscapedNewlineSequence3)
                        {
                            // loop again, we don't want empty escape chars
                            c = this.ReadStringChar(Characters.ProhibitionContext.None);
                            continue;
                        }

                        if (c == TextConstants.EscapeNotDefined)
                        {
                            throw new InvalidTokenException(c);
                        }

                        break;
                }

                // at this point we have a post-escaped character to return to the caller
                break;
            }

            if (c == CharacterSequence.CharSeqEof)
            {
                return c;
            }

            return c;
        }

        /// <summary>
        /// Replace the escaped character with the actual content.
        /// </summary>
        private int ReadEscapedCharContent(int c1, bool clob)
        {
            if (c1 < 0)
            {
                // if '\' is followed by a newline sequence we can just return in to ReadEscapedChar to be skipped
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
            {
                throw new InvalidTokenException(c1);
            }

            var c2 = TextConstants.GetEscapeReplacementCharacter(c1);
            switch (c2)
            {
                case TextConstants.EscapeNotDefined:
                    throw new InvalidTokenException(c2);
                case TextConstants.EscapeLittleU:
                    if (clob)
                    {
                        throw new InvalidTokenException(c1);
                    }

                    c2 = this.ReadHexEscapeSequence(4);
                    break;
                case TextConstants.EscapeBigU:
                    if (clob)
                    {
                        throw new InvalidTokenException(c1);
                    }

                    c2 = this.ReadHexEscapeSequence(8);
                    break;
                case TextConstants.EscapeHex:
                    c2 = this.ReadHexEscapeSequence(2);
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
                var c = this.ReadChar();
                if (c < 0)
                {
                    throw new UnexpectedEofException();
                }

                var d = TextConstants.HexDigitValue(c);
                if (d < 0)
                {
                    return -1;
                }

                hexchar = (hexchar << 4) + d;
            }

            if (len > 0)
            {
                throw new IonException("Hex digit len exceeded maximum");
            }

            return hexchar;
        }

        /// <summary>
        /// This will read the character unit in the context of a string, and will absorb escaped sequence.
        /// </summary>
        /// <returns>Read char.</returns>
        /// <remarks>This will NOT do encoding.</remarks>
        private int ReadStringChar(Characters.ProhibitionContext prohibitionContext)
        {
            var c = this.input.Read();
            if (Characters.IsProhibited(c, prohibitionContext))
            {
                throw new IonException($"Invalid character {(char)c}");
            }

            if (c == '\\' || c == '\n' || c == '\r')
            {
                c = this.EatNewLineSequence(c);
            }

            return c;
        }

        /// <summary>
        /// Depending on what the character is, read the new line sequence.
        /// </summary>
        /// <param name="c">Starting char.</param>
        /// <returns>The character code.</returns>
        private int EatNewLineSequence(int c)
        {
            int c2;
            switch (c)
            {
                default:
                    throw new InvalidOperationException($"Nothing to absorb after {(char)c}");
                case '\\':
                    c2 = this.input.Read();
                    switch (c2)
                    {
                        case '\r':
                            // DOS <cr><lf> (\r\n) or old Mac <cr>
                            var c3 = this.input.Read();
                            if (c3 == '\n')
                            {
                                // DOS <cr><lf> (\r\n)
                                return CharacterSequence.CharSeqEscapedNewlineSequence3;
                            }

                            // old Mac <cr>, fall back
                            this.UnreadChar(c3);
                            return CharacterSequence.CharSeqEscapedNewlineSequence2;
                        case '\n':
                            // Unix line feed
                            return CharacterSequence.CharSeqEscapedNewlineSequence1;
                        default:
                            // not a slash new line, so we'll just return the slash
                            // leave it to be handled elsewhere
                            this.UnreadChar(c2);
                            return c;
                    }

                case '\r':
                    c2 = this.input.Read();
                    if (c2 == '\n')
                    {
                        // DOS <cr><lf> (\r\n)
                        return CharacterSequence.CharSeqNewlineSequence3;
                    }

                    // old Mac <cr>, fall back
                    this.UnreadChar(c2);
                    return CharacterSequence.CharSeqNewlineSequence2;
                case '\n':
                    return CharacterSequence.CharSeqNewlineSequence1;
            }
        }

        /// <summary>
        /// Read a character unit, and the following new line sequence.
        /// </summary>
        /// <returns>The read character (or the new-line sequence).</returns>
        private int ReadChar()
        {
            var c = this.input.Read();
            if (c == '\n' || c == '\r')
            {
                c = this.EatNewLineSequence(c);
            }

            return c;
        }

        /// <summary>
        /// This will unread the current char and depending on that might unread several more char that belongs
        /// to the same sequence.
        /// </summary>
        /// <param name="c">Char to unread.</param>
        private void UnreadChar(int c)
        {
            if (c >= 0)
            {
                this.input.Unread(c);
                return;
            }

            switch (c)
            {
                default:
                    Debug.Assert(false, $"Invalid character encountered: {c}");
                    break;
                case CharacterSequence.CharSeqNewlineSequence1:
                    this.input.Unread('\n');
                    break;
                case CharacterSequence.CharSeqNewlineSequence2:
                    this.input.Unread('\r');
                    break;
                case CharacterSequence.CharSeqNewlineSequence3:
                    this.input.Unread('\n');
                    this.input.Unread('\r');
                    break;
                case CharacterSequence.CharSeqEscapedNewlineSequence1:
                    this.input.Unread('\n');
                    this.input.Unread('\\');
                    break;
                case CharacterSequence.CharSeqEscapedNewlineSequence2:
                    this.input.Unread('\r');
                    this.input.Unread('\\');
                    break;
                case CharacterSequence.CharSeqEscapedNewlineSequence3:
                    this.input.Unread('\n');
                    this.input.Unread('\r');
                    this.input.Unread('\\');
                    break;
                case CharacterSequence.CharSeqEof:
                    this.input.Unread(CharacterSequence.CharSeqEof);
                    break;
            }
        }

        /// <summary>
        /// Any CLOB with double quote, can hold only one " " and the only
        /// acceptable character after the second double quote is }.
        /// </summary>
        private void HandleClobsWithDoubleQuote()
        {
            var c = this.SkipOverWhiteSpace(CommentStrategy.Error);
            if (c != '}')
            {
                throw new IonException($"Bad Character in Clob:: {(char)c} , expected \"}}}}\"");
            }

            this.UnreadChar(c);
        }

        private void LoadFixedDigits(StringBuilder sb, int len)
        {
            int c;

            switch (len)
            {
                default:
                    while (len > 4)
                    {
                        c = this.ReadChar();
                        if (!char.IsDigit((char)c))
                        {
                            throw new InvalidTokenException(c);
                        }

                        sb.Append((char)c);
                        len--;
                    }

                    // fall through
                    goto case 4;
                case 4:
                    c = this.ReadChar();
                    if (!char.IsDigit((char)c))
                    {
                        throw new InvalidTokenException(c);
                    }

                    sb.Append((char)c);

                    // fall through
                    goto case 3;
                case 3:
                    c = this.ReadChar();
                    if (!char.IsDigit((char)c))
                    {
                        throw new InvalidTokenException(c);
                    }

                    sb.Append((char)c);

                    // fall through
                    goto case 2;
                case 2:
                    c = this.ReadChar();
                    if (!char.IsDigit((char)c))
                    {
                        throw new InvalidTokenException(c);
                    }

                    sb.Append((char)c);

                    // fall through
                    goto case 1;
                case 1:
                    c = this.ReadChar();
                    if (!char.IsDigit((char)c))
                    {
                        throw new InvalidTokenException(c);
                    }

                    sb.Append((char)c);
                    break;
            }
        }

        private IonType LoadTimestamp(StringBuilder sb, int c)
        {
            // we read the year in our caller, we should only be
            // here if we read 4 digits and then a dash or a 'T'
            Debug.Assert(c == '-' || c == 'T', "c is not '-' or 'T'");

            sb.Append((char)c);

            // if it's 'T' we done: yyyyT
            if (c == 'T')
            {
                c = this.ReadChar(); // because we'll unread it before we return
                return this.FinishLoadNumber(sb, c, TextConstants.TokenTimestamp);
            }

            // read month
            this.LoadFixedDigits(sb, 2);

            c = this.ReadChar();
            if (c == 'T')
            {
                sb.Append((char)c);
                c = this.ReadChar(); // because we'll unread it before we return
                return this.FinishLoadNumber(sb, c, TextConstants.TokenTimestamp);
            }

            if (c != '-')
            {
                throw new InvalidTokenException(c);
            }

            // read day
            sb.Append((char)c);
            this.LoadFixedDigits(sb, 2);

            // look for the 'T', otherwise we're done (and happy about it)
            c = this.ReadChar();
            if (c != 'T')
            {
                return this.FinishLoadNumber(sb, c, TextConstants.TokenTimestamp);
            }

            // so either we're done or we must at least hours and minutes
            // hour
            sb.Append((char)c);
            c = this.ReadChar();
            if (!char.IsDigit((char)c))
            {
                return this.FinishLoadNumber(sb, c, TextConstants.TokenTimestamp);
            }

            sb.Append((char)c);
            this.LoadFixedDigits(sb, 1); // we already read the first digit
            c = this.ReadChar();
            if (c != ':')
            {
                throw new InvalidTokenException(c);
            }

            // minutes
            sb.Append((char)c);
            this.LoadFixedDigits(sb, 2);
            c = this.ReadChar();
            if (c == ':')
            {
                // seconds are optional
                // and first we'll have the whole seconds
                sb.Append((char)c);
                this.LoadFixedDigits(sb, 2);
                c = this.ReadChar();
                if (c == '.')
                {
                    sb.Append((char)c);
                    c = this.ReadChar();

                    // Per spec and W3C Note http://www.w3.org/TR/NOTE-datetime
                    // We require at least one digit after the decimal point.
                    if (!char.IsDigit((char)c))
                    {
                        throw new InvalidTokenException(c);
                    }

                    c = this.LoadDigits(sb, c);
                }
            }

            // since we have a time, we have to have a timezone of some sort
            // the timezone offset starts with a '+' '-' 'Z' or 'z'
            if (c == 'z' || c == 'Z')
            {
                sb.Append((char)c);

                // read ahead since we'll check for a valid ending in a bit
                c = this.ReadChar();
            }
            else if (c == '+' || c == '-')
            {
                // then ... hours of time offset
                sb.Append((char)c);
                this.LoadFixedDigits(sb, 2);
                c = this.ReadChar();
                if (c != ':')
                {
                    // those hours need their minutes if it wasn't a 'z'
                    // (above) then it has to be a +/- hours { : minutes }
                    throw new InvalidTokenException(c);
                }

                // and finally the *not* optional minutes of time offset
                sb.Append((char)c);
                this.LoadFixedDigits(sb, 2);
                c = this.ReadChar();
            }
            else
            {
                // some sort of offset is required with a time value
                // if it wasn't a 'z' (above) then it has to be a +/- hours { : minutes }
                throw new InvalidTokenException(c);
            }

            return this.FinishLoadNumber(sb, c, TextConstants.TokenTimestamp);
        }

        private int LoadExponent(StringBuilder valueBuffer)
        {
            var c = this.ReadChar();
            if (c == '-' || c == '+')
            {
                valueBuffer.Append((char)c);
                c = this.ReadChar();
            }

            c = this.LoadDigits(valueBuffer, c);

            if (c != '.')
            {
                return c;
            }

            valueBuffer.Append((char)c);
            c = this.ReadChar();
            c = this.LoadDigits(valueBuffer, c);

            return c;
        }

        private int LoadDigits(StringBuilder sb, int c)
        {
            if (!char.IsDigit((char)c))
            {
                return c;
            }

            sb.Append((char)c);

            return this.ReadNumeric(sb, Radix.Decimal, NumericState.Digit);
        }

        /// <summary>
        /// <see cref="LoadNumber"/> will always read 1 extra terminating character, so we unread it here.
        /// </summary>
        /// <exception cref="InvalidTokenException">If the last character read is not a terminating char.</exception>
        private IonType FinishLoadNumber(StringBuilder numericText, int c, int token)
        {
            // all forms of numeric need to stop someplace rational
            if (!this.IsTerminatingCharacter(c))
            {
                throw new FormatException($"Numeric value [{numericText}] followed by invalid character: {(int)c}");
            }

            // we read off the end of the number, so put back
            // what we don't want, but what ever we have is an int
            this.UnreadChar(c);

            // also, mark the current 'number' token as finished so it doesnt get skipped over
            this.MarkTokenFinished();
            return TextConstants.GetIonTypeOfToken(token);
        }

        private int LoadRadixValue(StringBuilder sb, int c2, Radix radix)
        {
            sb.Append((char)c2);

            return this.ReadNumeric(sb, radix);
        }

        private int ReadNumeric(StringBuilder buffer, Radix radix) => this.ReadNumeric(buffer, radix, NumericState.Start);

        private int ReadNumeric(StringBuilder buffer, Radix radix, NumericState state)
        {
            while (true)
            {
                var c = this.ReadChar();
                switch (state)
                {
                    case NumericState.Start:
                        if (radix.IsValidDigit(c))
                        {
                            buffer.Append(radix.NormalizeDigit((char)c));
                            state = NumericState.Digit;
                        }
                        else
                        {
                            return c;
                        }

                        break;
                    case NumericState.Digit:
                        if (radix.IsValidDigit(c))
                        {
                            buffer.Append(radix.NormalizeDigit((char)c));
                            state = NumericState.Digit;
                        }
                        else if (c == '_')
                        {
                            state = NumericState.Underscore;
                        }
                        else
                        {
                            return c;
                        }

                        break;
                    case NumericState.Underscore:
                        if (radix.IsValidDigit(c))
                        {
                            buffer.Append(radix.NormalizeDigit((char)c));
                            state = NumericState.Digit;
                        }
                        else
                        {
                            this.UnreadChar(c);
                            return '_';
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(state), state, null);
                }
            }
        }

        private int ReadTripleQuotedChar(bool isClob)
        {
            var c = this.ReadStringChar(Characters.ProhibitionContext.LongChar);
            switch (c)
            {
                case '\'':
                    if (this.Is2SingleQuotes())
                    {
                        // clobs disallow comments everywhere within the value
                        var commentStrategy = isClob ? CommentStrategy.Error : CommentStrategy.Ignore;

                        // so at this point we are at the end of the closing
                        // triple quote - so we need to look ahead to see if
                        // there's just whitespace and a new opening triple quote
                        c = this.SkipOverWhiteSpace(commentStrategy);
                        if (c == '\'' && this.Is2SingleQuotes())
                        {
                            // there's another segment so read the next segment as well
                            // since we're now just before char 1 of the next segment
                            // loop again, but don't append this char
                            return CharacterSequence.CharSeqStringNonTerminator;
                        }

                        // at this point, we are at the end of the closing
                        // triple quote and it does not follow by any other '
                        // so the only acceptable charcter is the closing brace
                        if (isClob && c != '}')
                        {
                            throw new IonException($"Bad Character in Clob: {(char)c} , expected \"}}}}\"");
                        }

                        // end of last segment - we're done (although we read a bit too far)
                        this.UnreadChar(c);
                        c = CharacterSequence.CharSeqStringTerminator;
                    }

                    break;
                case '\\':
                    c = this.ReadEscapedChar(c, isClob);
                    break;
                case CharacterSequence.CharSeqEscapedNewlineSequence1:
                case CharacterSequence.CharSeqEscapedNewlineSequence2:
                case CharacterSequence.CharSeqEscapedNewlineSequence3:
                case CharacterSequence.CharSeqNewlineSequence1:
                case CharacterSequence.CharSeqNewlineSequence2:
                case CharacterSequence.CharSeqNewlineSequence3:
                    break;
                case -1:
                    break;
            }

            return c;
        }

        private void Require7BitChar(int c)
        {
            if (!Characters.Is7BitChar(c))
            {
                throw new IonException($"Illegal character: {(char)c}. All characters must be 7-bit ASCII");
            }
        }
    }
}
