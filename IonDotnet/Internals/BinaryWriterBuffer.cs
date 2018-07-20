using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace IonDotnet.Internals
{
    internal abstract class BinaryWriterBuffer : IDisposable
    {
        private readonly List<byte[]> _bufferBlocks;
        private byte[] _currentBlock;
        private List<Memory<byte>> _currentSequence;

        private int BlockSize => _currentBlock?.Length ?? 0;

        /// <summary>
        /// The smallest unwritten index of the <see cref="_currentBlock"/>
        /// <para>If this equals block size, that means current block is full</para>
        /// </summary>
        private int _runningIndex;

        /// <summary>
        /// Total bytes written since the last <see cref="Wrapup"/>
        /// </summary>
        private long _writtenSoFar;

        protected BinaryWriterBuffer(int blockSize)
        {
            //4 is just too small!
            Debug.Assert(blockSize >= 4);

            //TODO should we do early alloc?
            _currentBlock = ArrayPool<byte>.Shared.Rent(blockSize);
            _bufferBlocks = new List<byte[]> {_currentBlock};
        }

        /// <summary>
        /// Write all the characters in the Span to the buffer
        /// </summary>
        /// <param name="chars">The char sequence</param>
        /// <remarks>Commit to write all the characters, and will throw exception if bad things happen</remarks>
        public void WriteChars(ReadOnlySpan<char> chars)
        {
            //get the bytecount first
            var byteCount = Encoding.UTF8.GetByteCount(chars);
            if (byteCount > BlockSize - _runningIndex)
            {
                WriteCharsSlow(chars, byteCount);
                return;
            }

            // we fit!!!
            Encoding.UTF8.GetBytes(chars, new Span<byte>(_currentBlock, _runningIndex, byteCount));
            _runningIndex += byteCount;
            _writtenSoFar += byteCount;
        }

        /// <summary>
        /// The slow path, write with allocations
        /// </summary>
        /// <param name="chars">Char sequence</param>
        /// <param name="bytesToWrite">Total bytes needed to utf8-encode the string</param>
        private void WriteCharsSlow(ReadOnlySpan<char> chars, int bytesToWrite)
        {
            if (bytesToWrite <= IonConstants.ShortStringLength)
            {
                WriteShortChars(chars, bytesToWrite);
            }
            else
            {
                WriteLongChars(chars, bytesToWrite);
            }
        }

        private void WriteShortChars(ReadOnlySpan<char> chars, int bytesToWrite)
        {
            Span<byte> alloc = stackalloc byte[IonConstants.ShortStringLength];
            Encoding.UTF8.GetBytes(chars, alloc);
            WriteBytes(alloc.Slice(0, bytesToWrite));
        }

        private void WriteLongChars(ReadOnlySpan<char> chars, int bytesToWrite)
        {
            //TODO is there a better way?
            Span<byte> alloc = new byte[bytesToWrite];
            Encoding.UTF8.GetBytes(chars, alloc);
            WriteBytes(alloc);
        }

        /// <summary>
        /// This is called when _runningIndex reaches the end of the current block
        /// We gotta add the end segment to the list of current segment sequence
        /// </summary>
        private void AddBlockEndToCurrentSequence()
        {
            if (BlockSize >= _writtenSoFar)
            {
                Debug.Assert(_currentSequence == null);
                //this means that all the bytes written since the last wrapup() fits in one block
                _currentSequence = new List<Memory<byte>> {new Memory<byte>(_currentBlock, BlockSize - (int) _writtenSoFar, (int) _writtenSoFar)};
                return;
            }

            // _writtenSoFar > BlockSize means this whole block is to be added
            _currentSequence.Add(_currentBlock);
        }

        /// <summary>
        /// Write all <paramref name="bytes"/> to the buffer
        /// </summary>
        /// <param name="bytes">Bytes to write</param>
        private void WriteBytes(Span<byte> bytes)
        {
            var bytesToWrite = bytes.Length;
            while (bytesToWrite > 0)
            {
                // first, write what we can
                var left = BlockSize - _runningIndex;
                var bytesWritten = bytesToWrite > left ? left : bytesToWrite;
                bytes.Slice(0, bytesWritten).CopyTo(new Span<byte>(_currentBlock, _runningIndex, bytesWritten));
                _runningIndex += bytesWritten;
                _writtenSoFar += bytesWritten;
                bytesToWrite -= bytesWritten;

                Debug.Assert(_runningIndex <= BlockSize);
                if (bytesToWrite == 0) continue;

                Debug.Assert(_runningIndex == BlockSize);
                // new allocation needed
                AddBlockEndToCurrentSequence();
                var newBlock = ArrayPool<byte>.Shared.Rent(BlockSize);
                _bufferBlocks.Add(newBlock);
                _currentBlock = newBlock;
                _runningIndex = 0;
                bytes = bytes.Slice(bytesWritten);
            }
        }

        /// <summary>
        /// Wrap up the current write streak
        /// </summary>
        /// <returns>The sequence of written segments in the write streak</returns>
        public IList<Memory<byte>> Wrapup()
        {
            if (_runningIndex >= _writtenSoFar)
            {
                //this means that all the bytes written since the last wrapup() fits in one block
                //so just need to return that segment
                //make sure that we are conservative in array here
                Debug.Assert(_currentSequence == null);
                var l = new List<Memory<byte>>(1)
                {
                    new Memory<byte>(_currentBlock, _runningIndex - (int) _writtenSoFar, (int) _writtenSoFar)
                };
                _writtenSoFar = 0;
                return l;
            }

            //this means we have reached a new block since last wrapup(), return the current list
            _currentSequence.Add(new Memory<byte>(_currentBlock, 0, _runningIndex));
            var ret = _currentSequence;
            _currentSequence = null;
            _writtenSoFar = 0;
            return ret;
        }

        public void Dispose()
        {
            foreach (var block in _bufferBlocks)
            {
                ArrayPool<byte>.Shared.Return(block);
            }

            _bufferBlocks.Clear();
        }
    }

    internal class BigBlockBinaryWriterBuffer : BinaryWriterBuffer
    {
        private const int MyBlockSize = 512;

        public BigBlockBinaryWriterBuffer() : base(MyBlockSize)
        {
        }
    }
}
