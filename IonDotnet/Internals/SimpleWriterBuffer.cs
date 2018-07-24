using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace IonDotnet.Internals
{
    /// <inheritdoc />
    /// <summary>
    /// Simple careless write buffer, just use a memory stream
    /// </summary>
    internal sealed class SimpleWriterBuffer : IWriterBuffer
    {
        private readonly MemoryStream _memory = new MemoryStream();
        private long _writtenSoFar;
        private IList<Memory<byte>> _currentSequence;

        public int WriteUtf8(ReadOnlySpan<char> s, int length)
        {
            var byteCount = length == -1 ? Encoding.UTF8.GetByteCount(s) : length;
            Debug.Assert(length == -1 || length == Encoding.UTF8.GetByteCount(s));

            var span = new byte[byteCount];
            Encoding.UTF8.GetBytes(s, span);
            _memory.Write(span);
            _writtenSoFar += byteCount;
            return byteCount;
        }

        public void WriteByte(byte octet)
        {
            throw new NotImplementedException();
        }

        public void WriteUint8(long value)
        {
            throw new NotImplementedException();
        }

        public void WriteUint8(int value)
        {
            throw new NotImplementedException();
        }

        public void WriteUint16(long value)
        {
            throw new NotImplementedException();
        }

        public void WriteUint24(long value)
        {
            throw new NotImplementedException();
        }

        public void WriteUint32(long value)
        {
            throw new NotImplementedException();
        }

        public void WriteUint40(long value)
        {
            throw new NotImplementedException();
        }

        public void WriteUint48(long value)
        {
            throw new NotImplementedException();
        }

        public void WriteUint56(long value)
        {
            throw new NotImplementedException();
        }

        public void WriteUint64(long value)
        {
            throw new NotImplementedException();
        }

        public void WriteBytes(Span<byte> bytes)
        {
            _memory.Write(bytes);
            _writtenSoFar += bytes.Length;
        }

        public int WriteVarUint(long value)
        {
            throw new NotImplementedException();
        }

        public int WriteAnnotationsWithLength(IEnumerable<SymbolToken> annotations)
        {
            throw new NotImplementedException();
        }

        public void StartStreak(IList<Memory<byte>> sequence)
        {
            Debug.Assert(sequence != null);
            _currentSequence = sequence;
        }

        public IList<Memory<byte>> Wrapup()
        {
            Debug.Assert(_currentSequence != null);
            if (_writtenSoFar == 0) return _currentSequence;

            var buffer = new Memory<byte>(new byte[_writtenSoFar]);
            _memory.Seek(-_writtenSoFar, SeekOrigin.Current);
            _memory.Read(buffer.Span);
            _memory.Seek(_writtenSoFar, SeekOrigin.Current);
            _writtenSoFar = 0;
            _currentSequence.Add(buffer);

            var ret = _currentSequence;
            _currentSequence = null;
            return ret;
        }

        public void Dispose()
        {
            _memory.Dispose();
        }
    }
}
