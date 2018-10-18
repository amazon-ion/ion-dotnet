using System;
using System.Diagnostics;

namespace IonDotnet.Internals.Text
{
    internal class CharSequenceStream : TextStream
    {
        private readonly ReadOnlyMemory<char> _chars;
        private int _idx;


        public CharSequenceStream(string text) : this(text.AsMemory())
        {
        }

        public CharSequenceStream(ReadOnlyMemory<char> chars)
        {
            _chars = chars;
        }

        public override int Read() => _idx == _chars.Length ? -1 : _chars.Span[_idx++];

        public override void Unread(int c)
        {
            //EOF
            if (c == -1)
                return;
            //since we have access to the memory layout we can just reduce the index;
            Debug.Assert(_idx > 0);
            Debug.Assert(_chars.Span[_idx - 1] == c);
            _idx--;
        }
    }
}
