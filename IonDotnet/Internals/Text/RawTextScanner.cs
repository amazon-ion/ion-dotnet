using System;
using System.Collections.Generic;
using IonDotnet.Utils;

namespace IonDotnet.Internals.Text
{
    internal sealed class RawTextScanner
    {
        private readonly TextStream _input;
        private readonly bool _isByteData;

        private readonly Stack<int> _unreadStack;

        public RawTextScanner(TextStream input, bool isByteData)
        {
            _input = input;
            _isByteData = isByteData;
            _unreadStack = new Stack<int>();
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
            throw new NotImplementedException();
        }

        private int ReadChar()
        {
            if (_unreadStack?.Count > 0)
                return _unreadStack.Pop();

            return _input.Read();
        }

        /// <summary>
        /// This will unread the current char and depending on that might unread several more char that belongs
        /// to the same sequence
        /// </summary>
        /// <param name="c">Char to unread</param>
        private void UnreadChar(int c)
        {
            //TODO check for newline and escaped sequence
            _unreadStack.Push(c);
        }
    }
}
