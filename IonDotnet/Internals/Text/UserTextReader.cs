using System.IO;

namespace IonDotnet.Internals.Text
{
    internal class UserTextReader : SystemTextReader
    {
        public UserTextReader(string textForm, IonType parent = IonType.None)
            : base(new CharSequenceStream(textForm), parent)
        {
        }

        public UserTextReader(Stream utf8Stream, IonType parent = IonType.None)
            : base(new Utf8ByteStream(utf8Stream), parent)
        {
        }
    }
}
