using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace IonDotnet.Internals.Text
{
    /// <summary>
    /// Extends .NET <see cref="System.IO.StreamWriter"/> to include some writing functions
    /// </summary>
    internal class IonTextRawWriter
    {
        private readonly TextWriter _writer;


        private static readonly string[] ZeroPadding = {"", "0", "00", "000", "0000", "00000", "000000", "0000000",};

        /// <summary>
        /// Escapes for U+00 through U+FF, for use in double-quoted Ion strings. This includes escapes
        /// for all LATIN-1 code points U+80 through U+FF.
        /// </summary>
        private static readonly string[] StringEscapeCodes;

        private static readonly string[] LongStringEscapeCodes;

        private static readonly string[] SymbolEscapeCodes;

        private static readonly string[] JsonEscapeCodes;

        static IonTextRawWriter()
        {
            StringEscapeCodes = new string[256];
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

            LongStringEscapeCodes = new string[256];
            for (var i = 0; i < 256; ++i)
            {
                LongStringEscapeCodes[i] = StringEscapeCodes[i];
            }

            LongStringEscapeCodes['\n'] = null;
            LongStringEscapeCodes['\''] = "\\\'";
            LongStringEscapeCodes['\"'] = null; // Treat as normal code point for long string

            SymbolEscapeCodes = new string[256];
            for (var i = 0; i < 256; ++i)
            {
                SymbolEscapeCodes[i] = StringEscapeCodes[i];
            }

            SymbolEscapeCodes['\''] = "\\\'";
            SymbolEscapeCodes['\"'] = null; // Treat as normal code point for symbol.

            JsonEscapeCodes = new string[256];
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
                if (JsonEscapeCodes[i] == null)
                {
                    var s = $"{i:x}";
                    JsonEscapeCodes[i] = "\\u" + ZeroPadding[4 - s.Length] + s;
                }
            }

            for (var i = 0x7F; i < 0x100; ++i)
            {
                var s = $"{i:x}";
                JsonEscapeCodes[i] = "\\u00" + s;
            }
        }

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
            WriteStringWithEscapes(text);
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
            WriteStringWithEscapes(text);
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
            WriteStringWithEscapes(text);
            _writer.Write('\'');
        }

        /// <summary>
        /// Write symbol without any quotes
        /// </summary>
        /// <param name="text"></param>
        public void WriteSymbol(string text)
        {
            if (text == null)
            {
                _writer.Write("null.symbol");
                return;
            }

            WriteStringWithEscapes(text);
        }

        public void WriteLongString(string text)
        {
            if (text == null)
            {
                _writer.Write("null.string");
                return;
            }

            _writer.Write("'''");
            WriteStringWithEscapes(text);
            _writer.Write("'''");
        }

        public void WriteClobAsString(ReadOnlySpan<byte> clobBytes)
        {
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
        }

        private void WriteStringWithEscapes(string text)
        {
            //TODO handle different string types
            _writer.Write(text);
        }

        public void Write(char c) => _writer.Write(c);

        public void Write(int i) => _writer.Write(i);

        public void Write(string s) => _writer.Write(s);

        public Task FlushAsync() => _writer.FlushAsync();

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

            //TODO find a better way
            var str = d.ToString(CultureInfo.InvariantCulture);
            _writer.Write(str);

            if (!str.Contains("e") && !str.Contains("E"))
            {
                _writer.Write("e0");
            }
        }

        public void Write(in BigDecimal bd)
        {
            _writer.Write(bd.ToString());
        }
    }
}
