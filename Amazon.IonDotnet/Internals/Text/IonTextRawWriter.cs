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
    using System.Globalization;
    using System.IO;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Extends .NET <see cref="System.IO.StreamWriter"/> to include some writing functions.
    /// </summary>
    internal class IonTextRawWriter
    {
        private static readonly string[] ZeroPadding = { string.Empty, "0", "00", "000", "0000", "00000", "000000", "0000000" };

        /// <summary>
        /// Escapes for U+00 through U+FF, for use in double-quoted Ion strings. This includes escapes
        /// for all LATIN-1 code points U+80 through U+FF.
        /// </summary>
        private static readonly string[] StringEscapeCodes = new string[256];
        private static readonly string[] LongStringEscapeCodes = new string[256];
        private static readonly string[] SymbolEscapeCodes = new string[256];
        private static readonly string[] JsonEscapeCodes = new string[256];

        private readonly TextWriter writer;

        static IonTextRawWriter()
        {
            // short string
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
                {
                    continue;
                }

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

            // long string
            LongStringEscapeCodes['\n'] = null;
            LongStringEscapeCodes['\''] = "\\\'";
            LongStringEscapeCodes['\"'] = null; // Treat as normal code point for long string

            for (var i = 0; i < 256; ++i)
            {
                SymbolEscapeCodes[i] = StringEscapeCodes[i];
            }

            // symbols
            SymbolEscapeCodes['\''] = "\\\'";
            SymbolEscapeCodes['\"'] = null; // Treat as normal code point for symbol.

            // json
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
                if (JsonEscapeCodes[i] != null)
                {
                    continue;
                }

                var s = $"{i:x}";
                JsonEscapeCodes[i] = "\\u" + ZeroPadding[4 - s.Length] + s;
            }

            for (var i = 0x7F; i < 0x100; ++i)
            {
                var s = $"{i:x}";
                JsonEscapeCodes[i] = string.Format("\\u00{0}", s);
            }
        }

        public IonTextRawWriter(TextWriter writer)
        {
            this.writer = writer;
        }

        public void WriteJsonString(string text)
        {
            if (text == null)
            {
                this.writer.Write("null");
                return;
            }

            this.writer.Write('"');
            this.WriteStringWithEscapes(text, JsonEscapeCodes);
            this.writer.Write('"');
        }

        public void WriteString(string text)
        {
            if (text == null)
            {
                this.writer.Write("null.string");
                return;
            }

            this.writer.Write('"');
            this.WriteStringWithEscapes(text, StringEscapeCodes);
            this.writer.Write('"');
        }

        public void WriteSingleQuotedSymbol(string text)
        {
            if (text == null)
            {
                this.writer.Write("null.symbol");
                return;
            }

            this.writer.Write('\'');
            this.WriteStringWithEscapes(text, SymbolEscapeCodes);
            this.writer.Write('\'');
        }

        /// <summary>
        /// Write symbol without any quotes.
        /// </summary>
        /// <param name="text">symbol text to write.</param>
        public void WriteSymbol(string text)
        {
            if (text is null)
            {
                this.writer.Write("null.symbol");
                return;
            }

            this.WriteStringWithEscapes(text, SymbolEscapeCodes);
        }

        public void WriteLongString(string text)
        {
            if (text == null)
            {
                this.writer.Write("null.string");
                return;
            }

            this.writer.Write("'''");
            this.WriteStringWithEscapes(text, LongStringEscapeCodes);
            this.writer.Write("'''");
        }

        public void WriteClobAsString(ReadOnlySpan<byte> clobBytes)
        {
            this.writer.Write('"');
            foreach (var b in clobBytes)
            {
                var c = (char)(b & 0xff);
                var escapedByte = StringEscapeCodes[c];
                if (escapedByte != null)
                {
                    this.writer.Write(escapedByte);
                }
                else
                {
                    this.writer.Write(c);
                }
            }

            this.writer.Write('"');
        }

        public void Write(char c) => this.writer.Write(c);

        public void Write(int i) => this.writer.Write(i);

        public void Write(string s) => this.writer.Write(s);

        public void Flush() => this.writer.Flush();

        public void Write(long l) => this.writer.Write(l);

        public void Write(double d)
        {
            if (double.IsNaN(d))
            {
                this.writer.Write("nan");
                return;
            }

            if (double.IsPositiveInfinity(d))
            {
                this.writer.Write("+inf");
                return;
            }

            if (double.IsNegativeInfinity(d))
            {
                this.writer.Write("-inf");
                return;
            }

            string str;

            // Differentiate between negative zero and zero.
            if (d == 0 && BitConverter.DoubleToInt64Bits(d) < 0)
            {
                str = "-0e0";
                this.writer.Write(str);
            }
            else
            {
                // Using "R" round-trip format specifier.
                // Ensures the converted string can be parse back into the same numeric value.
                str = d.ToString("R");
                this.writer.Write(str);
            }

            foreach (var c in str)
            {
                if (c == 'e' || c == 'E')
                {
                    return;
                }
            }

            this.writer.Write("e0");
        }

        public void Write(decimal d)
        {
            var dString = d.ToString(CultureInfo.InvariantCulture);
            this.writer.Write(dString);
            foreach (var c in dString)
            {
                if (c == '.')
                {
                    return;
                }
            }

            this.writer.Write('d');
            this.writer.Write('0');
        }

        public void Write(in BigDecimal bd)
        {
            this.writer.Write(bd.ToString());
        }

        /// <summary>
        /// Write the substring as-is, meaning that no escaped sequence is written.
        /// This is for quickly writting a substring.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteSubstring(TextWriter writer, string s, int start, int length)
        {
            if (length == 0)
            {
                return;
            }

            var end = start + length;
            for (var i = start; i < end; i++)
            {
                writer.Write(s[i]);
            }
        }

        private void WriteStringWithEscapes(string text, string[] escapeTable)
        {
            for (int i = 0, l = text.Length; i < l; i++)
            {
                // find a span of non-escaped characters
                int j;
                var c = '\0';
                for (j = i; j < l; j++)
                {
                    c = text[j];
                    if (char.IsHighSurrogate(c))
                    {
                        // we found the high of a surrogate pair, just skip through the next char
                        j++;
                        continue;
                    }

                    if (c >= 0x100 || escapeTable[c] != null)
                    {
                        // we have a potential escaped sequence here, so skip
                        WriteSubstring(this.writer, text, i, j - i);
                        i = j;
                        break;
                    }
                }

                // the end, just write and exit
                if (j == l)
                {
                    WriteSubstring(this.writer, text, i, j - i);
                    break;
                }

                if (c < 0x80)
                {
                    // An escaped ASCII character.
                    Debug.Assert(escapeTable[c] != null, "escapeTable[c] is null");
                    this.writer.Write(escapeTable[c]);
                }
                else if (char.IsControl(c))
                {
                    // escape control sequence
                    this.writer.Write(escapeTable[c]);
                }
                else
                {
                    this.writer.Write(c);
                }
            }
        }
    }
}
