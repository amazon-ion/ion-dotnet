using System;
using System.IO;

namespace IonDotnet.Internals.Text
{
    internal class UserTextReader : SystemTextReader
    {
        public UserTextReader(string textForm, IonType parentType = IonType.None)
            : base(new CharSequenceStream(textForm), parentType)
        {
        }

        public UserTextReader(Stream utf8Stream, IonType parentType = IonType.None)
            : base(new Utf8ByteStream(utf8Stream), parentType)
        {
        }

        public UserTextReader(Stream utf8Stream, Span<byte> bytesRead, IonType parentType = IonType.None)
            : base(new Utf8ByteStream(utf8Stream, bytesRead), parentType)
        {
        }
    }
}
