using System.IO;

namespace IonDotnet.Internals.Text
{
    internal sealed partial class RawTextScanner
    {
        private class ByteTextStream : TextStream
        {
            private readonly Stream _inputStream;
            private readonly RawTextScanner _scanner;

            public ByteTextStream(Stream inputStream, RawTextScanner scanner)
            {
                _inputStream = inputStream;
                _scanner = scanner;
            }

            public override bool IsByteStream => true;

            public override int Read()
            {
                return _inputStream.ReadByte();
            }

            public override void Unread(int c)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
