using System.IO;

namespace IonDotnet.Internals.Text
{
    internal sealed class RawTextScanner
    {
        private readonly Stream _input;
        private readonly bool _isByteData;

        public RawTextScanner(Stream input, bool isByteData)
        {
            _input = input;
            _isByteData = isByteData;
        }
    }
}
