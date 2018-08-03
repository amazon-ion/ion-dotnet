using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace IonDotnet.Internals.Text
{
    internal class ByteTextStream : TextStream
    {
        private readonly Stream _inputStream;
        private readonly Stack<int> _unreadStack;

        public ByteTextStream(Stream inputStream)
        {
            if (!inputStream.CanRead)
                throw new ArgumentException("Inputstream must be readable", nameof(inputStream));

            _inputStream = inputStream;
            if (inputStream.CanSeek)
            {
                _unreadStack = new Stack<int>();
            }
        }

        public override bool IsByteStream => true;

        public override int Read()
        {
            if (_unreadStack != null && _unreadStack.Count > 0)
                return _unreadStack.Pop();

            return _inputStream.ReadByte();
        }

        public override void Unread(int c)
        {
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
