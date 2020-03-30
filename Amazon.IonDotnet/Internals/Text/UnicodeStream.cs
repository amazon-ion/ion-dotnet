using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Amazon.IonDotnet.Internals.Text
{
    internal class UnicodeStream : TextStream
    {
        private readonly StreamReader _streamReader;
        private readonly Stack<int> _unreadStack;
        private long? remainingChars = null;

        public UnicodeStream(Stream inputStream) : this(inputStream, Encoding.UTF8)
        {
        }

        public UnicodeStream(Stream inputStream, Encoding encoding)
        {
            if (!inputStream.CanRead)
                throw new ArgumentException("Input stream must be readable", nameof(inputStream));
            _streamReader = new StreamReader(inputStream, encoding);
            _unreadStack = new Stack<int>();
            if (inputStream.CanSeek)
            {
                remainingChars = inputStream.Length;
            }
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
                remainingChars = inputStream.Length;
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
            var value = _unreadStack.Count > 0 ? _unreadStack.Pop() : _streamReader.Read();

            if (remainingChars.HasValue)
            {
                remainingChars--;
                if (_streamReader.CurrentEncoding == Encoding.UTF8)
                {
                    IsValidUTF8Character();
                }
            }

            return value;
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

        private void IsValidUTF8Character()
        {
            if (remainingChars > 0 && _streamReader.Peek() == -1)
            {
                throw new IonException("Input stream is not a valid UTF-8 stream.");
            }
        }
    }
}
