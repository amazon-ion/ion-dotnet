using System;
using System.Collections.Generic;
using System.IO;

namespace IonDotnet.Internals.Text
{
    internal class Utf8ByteStream : TextStream
    {
        private readonly Stream _inputStream;
        private readonly Stack<int> _unreadStack;

        public Utf8ByteStream(Stream inputStream)
        {
            if (!inputStream.CanRead)
                throw new ArgumentException("Input stream must be readable", nameof(inputStream));

            _inputStream = inputStream;
            if (!inputStream.CanSeek)
            {
                _unreadStack = new Stack<int>();
            }
        }

        /// <summary>
        /// Create utf8 byte stream after already reading some bytes
        /// </summary>
        /// <param name="inputStream">Input stream</param>
        /// <param name="readBytes">Bytes read</param>
        public Utf8ByteStream(Stream inputStream, Span<byte> readBytes)
        {
            if (!inputStream.CanRead)
                throw new ArgumentException("Input stream must be readable", nameof(inputStream));

            _inputStream = inputStream;
            if (inputStream.CanSeek)
            {
                _inputStream.Seek(-readBytes.Length, SeekOrigin.Current);
                return;
            }

            _unreadStack = new Stack<int>(readBytes.Length);
            for (var i = readBytes.Length - 1; i >= 0; i--)
            {
                _unreadStack.Push(readBytes[i]);
            }
        }

        public override bool IsByteStream => true;

        public override int UnitSize => sizeof(byte);

        public override int Read()
        {
            if (_unreadStack != null && _unreadStack.Count > 0)
                return _unreadStack.Pop();

            return _inputStream.ReadByte();
        }

        public override void Unread(int c)
        {
            if (c == -1)
                return;

            if (_unreadStack != null)
            {
                _unreadStack.Push(c);
                return;
            }

            //if _unreadStack == null that means the stream is seek-able
            _inputStream.Seek(-1, SeekOrigin.Current);
        }
    }
}
