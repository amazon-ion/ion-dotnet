using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using IonDotnet.Utils;

namespace IonDotnet.Internals.Text
{
    internal class UnicodeStream : TextStream
    {
        private readonly StreamReader _streamReader;
        private readonly Stack<int> _unreadStack;

        public UnicodeStream(Stream inputStream) : this(inputStream, Encoding.UTF8)
        {
        }

        public UnicodeStream(Stream inputStream, Encoding encoding)
        {
            if (!inputStream.CanRead)
                throw new ArgumentException("Input stream must be readable", nameof(inputStream));

            _streamReader = new StreamReader(inputStream, encoding);
            _unreadStack = new Stack<int>();
        }

        public UnicodeStream(Stream inputStream, Span<byte> readBytes)
            : this(inputStream, Encoding.UTF8, readBytes)
        {
        }

        /// <summary>
        /// Create utf8 byte stream after already reading some bytes
        /// </summary>
        /// <param name="inputStream">Input stream</param>
        /// <param name="encoding">The type of encoding.</param>
        /// <param name="readBytes">Bytes read</param>
        public UnicodeStream(Stream inputStream, Encoding encoding, Span<byte> readBytes)
        {
            if (!inputStream.CanRead)
                throw new ArgumentException("Input stream must be readable", nameof(inputStream));

            _streamReader = new StreamReader(inputStream, encoding);
            if (inputStream.CanSeek)
            {
                InputStream.Seek(-readBytes.Length, SeekOrigin.Current);
                return;
            }

            _unreadStack = new Stack<int>(readBytes.Length);
            for (var i = readBytes.Length - 1; i >= 0; i--)
            {
                _unreadStack.Push(readBytes[i]);
            }
        }

        public override int Read()
        {
            return _unreadStack.Count > 0 ? _unreadStack.Pop() : _streamReader.Read();
        }

        public override void Unread(int c)
        {
            //make sure we never un-read any unicode character
//            Debug.Assert(Characters.Is7BitChar(c));

            if (c == -1)
                return;

            _unreadStack.Push(c);
        }

        private Stream InputStream => _streamReader.BaseStream;
    }
}
