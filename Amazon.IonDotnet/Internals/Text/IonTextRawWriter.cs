using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Amazon.IonDotnet.Internals.Text
{
    /// <summary>
    /// Extends .NET <see cref="System.IO.StreamWriter"/> to include some writing functions
    /// </summary>
    internal class IonTextRawWriter
    {
        private readonly TextWriter _writer;

        #region Escapes

        private static readonly string[] ZeroPadding = {"", "0", "00", "000", "0000", "00000", "000000", "0000000",};

        /// <summary>
        /// Escapes for U+00 through U+FF, for use in double-quoted Ion strings. This includes escapes
        /// for all LATIN-1 code points U+80 through U+FF.
        /// </summary>
        private static readonly string[] StringEscapeCodes = new string[256];

        private static readonly string[] LongStringEscapeCodes = new string[256];

        private static readonly string[] SymbolEscapeCodes = new string[256];

        private static readonly string[] JsonEscapeCodes = new string[256];

        static IonTextRawWriter()
        {
            //short string
            StringEscapeCodes[0x00] = "\\0";
            StringEscapeCodes[0x07] = "\\a";
            StringEscapeCodes[0x08] = "\\b";
            StringEscapeCodes['\t'] = "\\t";
            StringEscapeCodes['\n'] = "\\n";
            StringEscapeCodes[0x0B] = "\\v";
            StringEscapeCodes['\f'] = "\\f";
            StringEscapeCodes['\r'] = "\\r";
            StringEscapeCodes['\\'] = "\\\\";
            StringEscapeCodes['\"'] = "\\\"";
            for (var i = 1; i < 0x20; ++i)
            {
                if (StringEscapeCodes[i] != null)
                    continue;
                var s = $"{i:x}";
                StringEscapeCodes[i] = "\\x" + ZeroPadding[2 - s.Length] + s;
            }

            for (var i = 0x7F; i < 0x100; ++i)
            {
                var s = $"{i:x}";
                StringEscapeCodes[i] = "\\x" + s;
            }

            for (var i = 0; i < 256; ++i)
            {
                LongStringEscapeCodes[i] = StringEscapeCodes[i];
            }

            //long string
            LongStringEscapeCodes['\n'] = null;
            LongStringEscapeCodes['\''] = "\\\'";
            LongStringEscapeCodes['\"'] = null; // Treat as normal code point for long string

            for (var i = 0; i < 256; ++i)
            {
                SymbolEscapeCodes[i] = StringEscapeCodes[i];
            }

            //symbols
            SymbolEscapeCodes['\''] = "\\\'";
            SymbolEscapeCodes['\"'] = null; // Treat as normal code point for symbol.

            //json
            JsonEscapeCodes[0x08] = "\\b";
            JsonEscapeCodes['\t'] = "\\t";
            JsonEscapeCodes['\n'] = "\\n";
            JsonEscapeCodes['\f'] = "\\f";
            JsonEscapeCodes['\r'] = "\\r";
            JsonEscapeCodes['\\'] = "\\\\";
            JsonEscapeCodes['\"'] = "\\\"";

            // JSON requires all of these characters to be escaped.
            for (var i = 0; i < 0x20; ++i)
            {
                if (JsonEscapeCodes[i] != null) continue;

                var s = $"{i:x}";
                JsonEscapeCodes[i] = "\\u" + ZeroPadding[4 - s.Length] + s;
            }

            for (var i = 0x7F; i < 0x100; ++i)
            {
                var s = $"{i:x}";
                JsonEscapeCodes[i] = string.Format("\\u00{0}", s);
            }
        }

        #endregion


        public IonTextRawWriter(TextWriter writer)
        {
            _writer = writer;
        }

        public void WriteJsonString(string text)
        {
            if (text == null)
            {
                _writer.Write("null");
                return;
            }

            _writer.Write('"');
            WriteStringWithEscapes(text, JsonEscapeCodes);
            _writer.Write('"');
        }

        public void WriteString(string text)
        {
            if (text == null)
            {
                _writer.Write("null.string");
                return;
            }

            _writer.Write('"');
            WriteStringWithEscapes(text, StringEscapeCodes);
            _writer.Write('"');
        }

        public void WriteSingleQuotedSymbol(string text)
        {
            if (text == null)
            {
                _writer.Write("null.symbol");
                return;
            }

            _writer.Write('\'');
            WriteStringWithEscapes(text, SymbolEscapeCodes);
            _writer.Write('\'');
        }

        /// <summary>
        /// Write symbol without any quotes
        /// </summary>
        /// <param name="text"></param>
        public void WriteSymbol(string text)
        {
            if (text is null)
            {
                _writer.Write("null.symbol");
                return;
            }

            WriteStringWithEscapes(text, SymbolEscapeCodes);
        }

        public void WriteLongString(string text)
        {
            if (text == null)
            {
                _writer.Write("null.string");
                return;
            }

            _writer.Write("'''");
            WriteStringWithEscapes(text, LongStringEscapeCodes);
            _writer.Write("'''");
        }

        public void WriteClobAsString(ReadOnlySpan<byte> clobBytes)
        {
            _writer.Write('"');
            foreach (var b in clobBytes)
            {
                var c = (char) (b & 0xff);
                var escapedByte = StringEscapeCodes[c];
                if (escapedByte != null)
                {
                    _writer.Write(escapedByte);
                }
                else
                {
                    _writer.Write(c);
                }
            }

            _writer.Write('"');
        }

        public void Write(char c) => _writer.Write(c);

        public void Write(int i) => _writer.Write(i);

        public void Write(string s) => _writer.Write(s);

//        public Task FlushAsync() => _writer.FlushAsync();

        public void Flush() => _writer.Flush();

        public void Write(long l) => _writer.Write(l);

        public void Write(double d)
        {
            if (double.IsNaN(d))
            {
                _writer.Write("nan");
                return;
            }

            if (double.IsPositiveInfinity(d))
            {
                _writer.Write("+inf");
                return;
            }

            if (double.IsNegativeInfinity(d))
            {
                _writer.Write("-inf");
                return;
            }

            String str;

            // Differentiate between negative zero and zero.
            if (d == 0 && BitConverter.DoubleToInt64Bits(d) < 0)
            {
                str = "-0e0";
                _writer.Write(str);
            }
            else
            {
                str = d.ToString("R");
                _writer.Write(str);
            }

            foreach (var c in str)
            {
                if (c == 'e' || c == 'E')
                {
                    return;
                }
            }

            _writer.Write("e0");
        }

        public void Write(decimal d)
        {
            var dString = d.ToString(CultureInfo.InvariantCulture);
            _writer.Write(dString);
            foreach (var c in dString)
            {
                if (c == '.')
                {
                    return;
                }
            }

            _writer.Write('d');
            _writer.Write('0');
        }

        public void Write(in BigDecimal bd)
        {
            _writer.Write(bd.ToString());
        }

        private void WriteStringWithEscapes(string text, string[] escapeTable)
        {
            for (int i = 0, l = text.Length; i < l; i++)
            {
                //find a span of non-escaped characters
                int j;
                var c = '\0';
                for (j = i; j < l; j++)
                {
                    c = text[j];
                    if (char.IsHighSurrogate(c))
                    {
                        //we found the high of a surrogate pair, just skip through the next char
                        j++;
                        continue;
                    }

                    if (c >= 0x100 || escapeTable[c] != null)
                    {
                        //we have a potential escaped sequence here, so skip
                        WriteSubstring(_writer, text, i, j - i);
                        i = j;
                        break;
                    }
                }

                //the end, just write and exit
                if (j == l)
                {
                    WriteSubstring(_writer, text, i, j - i);
                    break;
                }

                if (c < 0x80)
                {
                    // An escaped ASCII character.
                    Debug.Assert(escapeTable[c] != null);
                    _writer.Write(escapeTable[c]);
                }
                else if (char.IsControl(c))
                {
                    //escape control sequence
                    _writer.Write(escapeTable[c]);
                }
                else
                {
                    _writer.Write(c);
                }
            }
        }

        /// <summary>
        /// Write the substring as-is, meaning that no escaped sequence is written.
        /// This is for quickly writting a substring
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteSubstring(TextWriter writer, string s, int start, int length)
        {
            if (length == 0)
            {
                return;
            }
#if NETCOREAPP2_1
            writer.Write(s.AsSpan().Slice(start, length));
#else
//this is weird but better than just writting a sub string 
            var end = start + length;
            for (var i = start; i < end; i++)
            {
                writer.Write(s[i]);
            }
#endif
        }
    }
}
