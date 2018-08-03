using System;
using System.Text;
using IonDotnet.Utils;
using static IonDotnet.Internals.Text.TextConstants;

namespace IonDotnet.Internals.Text
{
    internal sealed partial class RawTextScanner
    {
        private readonly TextStream _input;
        private bool isClob;

        public RawTextScanner(TextStream input)
        {
            _input = input;
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
        private int LoadDoubleQuotedString(StringBuilder sb)
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

        private int ReadEscapedChar(int c, bool clob)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This will read the character in the context of a string, and will absorb escaped sequence
        /// </summary>
        /// <returns>Read char</returns>
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
                        return CharacterSequence.CharSeqEscapedNewlineSequence3;
                    }

                    //old Mac <cr> , fall back
                    UnreadChar(c2);
                    return CharacterSequence.CharSeqEscapedNewlineSequence2;
                case '\n':
                    return CharacterSequence.CharSeqEscapedNewlineSequence1;
            }
        }

        private int ReadChar()
        {
            return _input.Read();
            //TODO handle eating newline sequences
        }

        /// <summary>
        /// This will unread the current char and depending on that might unread several more char that belongs
        /// to the same sequence
        /// </summary>
        /// <param name="c">Char to unread</param>
        private void UnreadChar(int c)
        {
            //TODO check for newline and escaped sequence
            _input.Unread(c);
        }
    }
}
