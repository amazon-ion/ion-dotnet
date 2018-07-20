using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IonDotnet.Internals
{
    /// <summary>
    /// Simple careless write buffer, just use a memory stream
    /// </summary>
    internal sealed class SimpleWriteBuffer : IWriteBuffer
    {
        private readonly MemoryStream _memory = new MemoryStream();
        private long _writtenSoFar;

        public int WriteUtf8(string s)
        {
            var bytes = Encoding.UTF8.GetBytes(s);
            _memory.Write(bytes);
            _writtenSoFar += bytes.Length;
            return bytes.Length;
        }

        public void WriteBytes(Span<byte> bytes)
        {
            _memory.Write(bytes);
            _writtenSoFar += bytes.Length;
        }

        public IList<Memory<byte>> Wrapup()
        {
            if (_writtenSoFar == 0) return new Memory<byte>[0];

            var buffer = new Memory<byte>(new byte[_writtenSoFar]);
            _memory.Seek(-_writtenSoFar, SeekOrigin.Current);
            _memory.Read(buffer.Span);
            _memory.Seek(_writtenSoFar, SeekOrigin.Current);
            _writtenSoFar = 0;
            return new List<Memory<byte>> {buffer};
        }

        public void Dispose()
        {
            _memory.Dispose();
        }
    }
}
