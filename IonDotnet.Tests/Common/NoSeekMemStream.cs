using System;
using System.IO;

namespace IonDotnet.Tests.Common
{
    /// <summary>
    /// A in-memory test stream that disable seeking
    /// </summary>
    public class NoSeekMemStream : Stream
    {
        private readonly MemoryStream _memoryStream;

        public NoSeekMemStream(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            _memoryStream = new MemoryStream(data);
        }

        public override void Flush() => _memoryStream.Flush();

        public override int Read(byte[] buffer, int offset, int count) => _memoryStream.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => throw new InvalidOperationException();

        public override void SetLength(long value) => throw new InvalidOperationException();

        public override void Write(byte[] buffer, int offset, int count) => _memoryStream.Write(buffer, offset, count);

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _memoryStream.Length;

        public override long Position
        {
            get => _memoryStream.Position;
            set => throw new InvalidOperationException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _memoryStream.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
