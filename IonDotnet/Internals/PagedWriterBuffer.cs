using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace IonDotnet.Internals
{
    internal abstract class PagedWriterBuffer : IWriteBuffer
    {
        private const int Shift1Byte = 8;
        private const int Shift2Byte = 8 * 2;
        private const int Shift3Byte = 8 * 3;
        private const int Shift4Byte = 8 * 4;
        private const int Shift5Byte = 8 * 5;
        private const int Shift6Byte = 8 * 6;
        private const int Shift7Byte = 8 * 7;

        private readonly List<byte[]> _bufferBlocks;
        private byte[] _currentBlock;
        private IList<Memory<byte>> _currentSequence;

        /// <summary>
        /// This is only a reference to the 'supposed' primary block size
        /// </summary>
        private readonly int _blockSize;

        /// <summary>
        /// The smallest unwritten index of the <see cref="_currentBlock"/>
        /// <para>If this equals block size, that means current block is full</para>
        /// </summary>
        private int _runningIndex;

        /// <summary>
        /// Total bytes written since the last <see cref="Wrapup"/>
        /// </summary>
        private long _writtenSoFar;

        protected PagedWriterBuffer(int intendedBlockSize)
        {
            //4 is just too small!
            Debug.Assert(intendedBlockSize >= 4);

            //TODO should we do early alloc?
            _currentBlock = ArrayPool<byte>.Shared.Rent(intendedBlockSize);
            _bufferBlocks = new List<byte[]> {_currentBlock};
            _blockSize = _currentBlock.Length;
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
            Debug.Assert(_currentSequence != null);
            if (_runningIndex >= _writtenSoFar)
            {
                //this means that all the bytes written since the last wrapup() fits in one block
                _currentSequence.Add(new Memory<byte>(_currentBlock, _runningIndex - (int) _writtenSoFar, (int) _writtenSoFar));
                return;
            }

            // _writtenSoFar > BlockSize means this whole block is to be added
            _currentSequence.Add(_currentBlock);
        }

        /// <summary>
        /// Allocate new memory block and update related fields
        /// </summary>
        private void AllocateNewBlock()
        {
            var newBlock = ArrayPool<byte>.Shared.Rent(_blockSize);
            _bufferBlocks.Add(newBlock);
            _currentBlock = newBlock;
            _runningIndex = 0;
        }

        public int WriteUtf8(ReadOnlySpan<char> chars, int length)
        {
            //get the bytecount first
            var byteCount = length == -1 ? Encoding.UTF8.GetByteCount(chars) : length;
            Debug.Assert(length == -1 || length == Encoding.UTF8.GetByteCount(chars));

            if (byteCount > _currentBlock.Length - _runningIndex)
            {
                WriteCharsSlow(chars, byteCount);
                return byteCount;
            }

            // we fit!!!
            Encoding.UTF8.GetBytes(chars, new Span<byte>(_currentBlock, _runningIndex, byteCount));
            _runningIndex += byteCount;
            _writtenSoFar += byteCount;
            return byteCount;
        }

        public void WriteByte(byte octet)
        {
            if (_runningIndex == _currentBlock.Length)
            {
                AllocateNewBlock();
            }

            _currentBlock[_runningIndex++] = octet;
            _writtenSoFar++;
        }

        public void WriteUint8(long value) => WriteByte((byte) value);

        public void WriteUint16(long value)
        {
            if (_currentBlock.Length - _runningIndex < 2)
            {
                WriteByte((byte) (value >> Shift1Byte));
                WriteByte((byte) value);
                return;
            }

            _currentBlock[_runningIndex++] = (byte) (value >> Shift1Byte);
            _currentBlock[_runningIndex++] = (byte) value;
            _writtenSoFar += 2;
        }

        public void WriteUint24(long value)
        {
            if (_currentBlock.Length - _runningIndex < 3)
            {
                WriteByte((byte) (value >> Shift2Byte));
                WriteByte((byte) (value >> Shift1Byte));
                WriteByte((byte) value);
                return;
            }

            _currentBlock[_runningIndex++] = (byte) (value >> Shift2Byte);
            _currentBlock[_runningIndex++] = (byte) (value >> Shift1Byte);
            _currentBlock[_runningIndex++] = (byte) value;
            _writtenSoFar += 3;
        }

        public void WriteUint32(long value)
        {
            if (_currentBlock.Length - _runningIndex < 4)
            {
                WriteByte((byte) (value >> Shift3Byte));
                WriteByte((byte) (value >> Shift2Byte));
                WriteByte((byte) (value >> Shift1Byte));
                WriteByte((byte) value);
                return;
            }

            _currentBlock[_runningIndex++] = (byte) (value >> Shift3Byte);
            _currentBlock[_runningIndex++] = (byte) (value >> Shift2Byte);
            _currentBlock[_runningIndex++] = (byte) (value >> Shift1Byte);
            _currentBlock[_runningIndex++] = (byte) value;
            _writtenSoFar += 4;
        }

        public void WriteUint40(long value)
        {
            if (_currentBlock.Length - _runningIndex < 5)
            {
                WriteByte((byte) (value >> Shift4Byte));
                WriteByte((byte) (value >> Shift3Byte));
                WriteByte((byte) (value >> Shift2Byte));
                WriteByte((byte) (value >> Shift1Byte));
                WriteByte((byte) value);
                return;
            }

            _currentBlock[_runningIndex++] = (byte) (value >> Shift4Byte);
            _currentBlock[_runningIndex++] = (byte) (value >> Shift3Byte);
            _currentBlock[_runningIndex++] = (byte) (value >> Shift2Byte);
            _currentBlock[_runningIndex++] = (byte) (value >> Shift1Byte);
            _currentBlock[_runningIndex++] = (byte) value;
            _writtenSoFar += 5;
        }

        public void WriteUint48(long value)
        {
            if (_currentBlock.Length - _runningIndex < 6)
            {
                WriteByte((byte) (value >> Shift5Byte));
                WriteByte((byte) (value >> Shift4Byte));
                WriteByte((byte) (value >> Shift3Byte));
                WriteByte((byte) (value >> Shift2Byte));
                WriteByte((byte) (value >> Shift1Byte));
                WriteByte((byte) value);
                return;
            }

            _currentBlock[_runningIndex++] = (byte) (value >> Shift5Byte);
            _currentBlock[_runningIndex++] = (byte) (value >> Shift4Byte);
            _currentBlock[_runningIndex++] = (byte) (value >> Shift3Byte);
            _currentBlock[_runningIndex++] = (byte) (value >> Shift2Byte);
            _currentBlock[_runningIndex++] = (byte) (value >> Shift1Byte);
            _currentBlock[_runningIndex++] = (byte) value;
            _writtenSoFar += 6;
        }

        public void WriteUint56(long value)
        {
            if (_currentBlock.Length - _runningIndex < 7)
            {
                WriteByte((byte) (value >> Shift6Byte));
                WriteByte((byte) (value >> Shift5Byte));
                WriteByte((byte) (value >> Shift4Byte));
                WriteByte((byte) (value >> Shift3Byte));
                WriteByte((byte) (value >> Shift2Byte));
                WriteByte((byte) (value >> Shift1Byte));
                WriteByte((byte) value);
                return;
            }

            _currentBlock[_runningIndex++] = (byte) (value >> Shift6Byte);
            _currentBlock[_runningIndex++] = (byte) (value >> Shift5Byte);
            _currentBlock[_runningIndex++] = (byte) (value >> Shift4Byte);
            _currentBlock[_runningIndex++] = (byte) (value >> Shift3Byte);
            _currentBlock[_runningIndex++] = (byte) (value >> Shift2Byte);
            _currentBlock[_runningIndex++] = (byte) (value >> Shift1Byte);
            _currentBlock[_runningIndex++] = (byte) value;
            _writtenSoFar += 7;
        }

        public void WriteUint64(long value)
        {
            if (_currentBlock.Length - _runningIndex < 8)
            {
                WriteByte((byte) (value >> Shift7Byte));
                WriteByte((byte) (value >> Shift6Byte));
                WriteByte((byte) (value >> Shift5Byte));
                WriteByte((byte) (value >> Shift4Byte));
                WriteByte((byte) (value >> Shift3Byte));
                WriteByte((byte) (value >> Shift2Byte));
                WriteByte((byte) (value >> Shift1Byte));
                WriteByte((byte) value);
                return;
            }

            _currentBlock[_runningIndex++] = (byte) (value >> Shift7Byte);
            _currentBlock[_runningIndex++] = (byte) (value >> Shift6Byte);
            _currentBlock[_runningIndex++] = (byte) (value >> Shift5Byte);
            _currentBlock[_runningIndex++] = (byte) (value >> Shift4Byte);
            _currentBlock[_runningIndex++] = (byte) (value >> Shift3Byte);
            _currentBlock[_runningIndex++] = (byte) (value >> Shift2Byte);
            _currentBlock[_runningIndex++] = (byte) (value >> Shift1Byte);
            _currentBlock[_runningIndex++] = (byte) value;
            _writtenSoFar += 7;
        }

        public void WriteBytes(Span<byte> bytes)
        {
            var bytesToWrite = bytes.Length;
            while (bytesToWrite > 0)
            {
                // first, write what we can
                var left = _blockSize - _runningIndex;
                var bytesWritten = bytesToWrite > left ? left : bytesToWrite;
                bytes.Slice(0, bytesWritten).CopyTo(new Span<byte>(_currentBlock, _runningIndex, bytesWritten));
                _runningIndex += bytesWritten;
                _writtenSoFar += bytesWritten;
                bytesToWrite -= bytesWritten;

                Debug.Assert(_runningIndex <= _blockSize);
                if (bytesToWrite == 0) continue;

                Debug.Assert(_runningIndex == _blockSize);
                // new allocation needed
                AddBlockEndToCurrentSequence();
                AllocateNewBlock();
                bytes = bytes.Slice(bytesWritten);
            }
        }

        /**
         * Ok, here's the logic for writing self-delimited int
         * Each byte uses (up to) 7 (lower) bits to store the value and 1 (highest) bit for the 'stop' flag
         * (1 if the value stops, 0 if not). So IntBitsPerOctet is the shift unit.
         * So if the value is larger than VarUintShift{X}UnitMinValue, we need to shift VarUintShift{X}Unit, then
         * bitwise-and VarUintUnitFlag to write that unit.
         * The final octet has to has the 'stop' flag set to 1 so x-or it with VarIntFinalOctetMask
         */

        private const int IntBitsPerOctet = 7;
        private const int VarUintUnitFlag = 0b_0111_1111;
        private const int VarUintShift8Unit = 8 * IntBitsPerOctet;
        private const int VarUintShift7Unit = 7 * IntBitsPerOctet;
        private const int VarUintShift6Unit = 6 * IntBitsPerOctet;
        private const int VarUintShift5Unit = 5 * IntBitsPerOctet;
        private const int VarUintShift4Unit = 4 * IntBitsPerOctet;
        private const int VarUintShift3Unit = 3 * IntBitsPerOctet;
        private const int VarUintShift2Unit = 2 * IntBitsPerOctet;
        private const int VarUintShift1Unit = IntBitsPerOctet;
        private const long VarUintShift8UnitMinValue = 1L << VarUintShift8Unit;
        private const long VarUintShift7UnitMinValue = 1L << VarUintShift7Unit;
        private const long VarUintShift6UnitMinValue = 1L << VarUintShift6Unit;
        private const long VarUintShift5UnitMinValue = 1L << VarUintShift5Unit;
        private const long VarUintShift4UnitMinValue = 1L << VarUintShift4Unit;
        private const long VarUintShift3UnitMinValue = 1L << VarUintShift3Unit;
        private const long VarUintShift2UnitMinValue = 1L << VarUintShift2Unit;
        private const long VarUintShift1UnitMinValue = 1L << VarUintShift1Unit;

        private const int VarIntFinalOctetMask = 0b_1000_0000;

        public int WriteVarUint(long value)
        {
            if (value < VarUintShift1UnitMinValue)
            {
                //fits in 1 byte
                WriteUint8((value & VarUintUnitFlag) | VarIntFinalOctetMask);
                return 1;
            }

            if (value < VarUintShift2UnitMinValue)
                return _currentBlock.Length - _runningIndex > 2
                    ? WriteVarUIntDirect2(value)
                    : WriteVarUIntSlow(value);

            if (value < VarUintShift3UnitMinValue)
                return _currentBlock.Length - _runningIndex > 3
                    ? WriteVarUIntDirect3(value)
                    : WriteVarUIntSlow(value);

            if (value < VarUintShift4UnitMinValue)
                return _currentBlock.Length - _runningIndex > 4
                    ? WriteVarUIntDirect4(value)
                    : WriteVarUIntSlow(value);

            if (value < VarUintShift5UnitMinValue)
                return _currentBlock.Length - _runningIndex > 5
                    ? WriteVarUIntDirect5(value)
                    : WriteVarUIntSlow(value);

            return WriteVarUIntSlow(value);
        }

        public int WriteAnnotationsWithLength(IEnumerable<SymbolToken> annotations)
        {
            //remember the current position to write the length
            //annotation length MUST fit in 1 byte
            if (_runningIndex == _currentBlock.Length)
            {
                AllocateNewBlock();
            }

            var lengthPosIdx = _runningIndex++;
            var lengthPosBlock = _currentBlock;
            var annotLength = 0;
            foreach (var symbol in annotations)
            {
                annotLength += WriteVarUint(symbol.Sid);
                if (annotLength > IonConstants.MaxAnnotationLength) throw new IonException($"Annotation length too large: {annotLength}");
            }

            _writtenSoFar += annotLength + 1;
            lengthPosBlock[lengthPosIdx] = (byte) ((annotLength & VarUintUnitFlag) | VarIntFinalOctetMask);
            return annotLength + 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int WriteVarUIntDirect2(long value)
        {
            _currentBlock[_runningIndex++] = (byte) ((value >> VarUintShift1Unit) & VarUintUnitFlag);
            _currentBlock[_runningIndex++] = (byte) ((value & VarUintUnitFlag) | VarIntFinalOctetMask);
            _writtenSoFar += 2;
            return 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int WriteVarUIntDirect3(long value)
        {
            _currentBlock[_runningIndex++] = (byte) ((value >> VarUintShift2Unit) & VarUintUnitFlag);
            _currentBlock[_runningIndex++] = (byte) ((value >> VarUintShift1Unit) & VarUintUnitFlag);
            _currentBlock[_runningIndex++] = (byte) ((value & VarUintUnitFlag) | VarIntFinalOctetMask);
            _writtenSoFar += 3;
            return 3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int WriteVarUIntDirect4(long value)
        {
            _currentBlock[_runningIndex++] = (byte) ((value >> VarUintShift3Unit) & VarUintUnitFlag);
            _currentBlock[_runningIndex++] = (byte) ((value >> VarUintShift2Unit) & VarUintUnitFlag);
            _currentBlock[_runningIndex++] = (byte) ((value >> VarUintShift1Unit) & VarUintUnitFlag);
            _currentBlock[_runningIndex++] = (byte) ((value & VarUintUnitFlag) | VarIntFinalOctetMask);
            _writtenSoFar += 4;
            return 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int WriteVarUIntDirect5(long value)
        {
            _currentBlock[_runningIndex++] = (byte) ((value >> VarUintShift4Unit) & VarUintUnitFlag);
            _currentBlock[_runningIndex++] = (byte) ((value >> VarUintShift3Unit) & VarUintUnitFlag);
            _currentBlock[_runningIndex++] = (byte) ((value >> VarUintShift2Unit) & VarUintUnitFlag);
            _currentBlock[_runningIndex++] = (byte) ((value >> VarUintShift1Unit) & VarUintUnitFlag);
            _currentBlock[_runningIndex++] = (byte) ((value & VarUintUnitFlag) | VarIntFinalOctetMask);
            _writtenSoFar += 5;
            return 5;
        }

        private int WriteVarUIntSlow(long value)
        {
            var size = 1;
            if (value >= VarUintShift8UnitMinValue)
            {
                WriteUint8((value >> VarUintShift8Unit) & VarUintUnitFlag);
                size++;
            }

            if (value >= VarUintShift7UnitMinValue)
            {
                WriteUint8((value >> VarUintShift7Unit) & VarUintUnitFlag);
                size++;
            }

            if (value >= VarUintShift6UnitMinValue)
            {
                WriteUint8((value >> VarUintShift6Unit) & VarUintUnitFlag);
                size++;
            }

            if (value >= VarUintShift5UnitMinValue)
            {
                WriteUint8((value >> VarUintShift5Unit) & VarUintUnitFlag);
                size++;
            }

            if (value >= VarUintShift4UnitMinValue)
            {
                WriteUint8((value >> VarUintShift4Unit) & VarUintUnitFlag);
                size++;
            }

            if (value >= VarUintShift3UnitMinValue)
            {
                WriteUint8((value >> VarUintShift3Unit) & VarUintUnitFlag);
                size++;
            }

            if (value >= VarUintShift2UnitMinValue)
            {
                WriteUint8((value >> VarUintShift2Unit) & VarUintUnitFlag);
                size++;
            }

            if (value >= VarUintShift1UnitMinValue)
            {
                WriteUint8((value >> VarUintShift1Unit) & VarUintUnitFlag);
                size++;
            }

            WriteUint8((value & VarUintUnitFlag) | VarIntFinalOctetMask);
            return size;
        }

        public void StartStreak(IList<Memory<byte>> sequence)
        {
            _currentSequence = sequence;
        }

        public IList<Memory<byte>> Wrapup()
        {
            Debug.Assert(_currentSequence != null);
            if (_runningIndex >= _writtenSoFar)
            {
                //this means that all the bytes written since the last wrapup() fits in one block
                //so just need to return that segment
                //make sure that we are conservative in array here
                _currentSequence.Add(new Memory<byte>(_currentBlock, _runningIndex - (int) _writtenSoFar, (int) _writtenSoFar));
                _writtenSoFar = 0;
            }
            else
            {
                //this means we have reached a new block since last wrapup(), and all previous segments have been added
                //we just need to add the current segment
                _currentSequence.Add(new Memory<byte>(_currentBlock, 0, _runningIndex));
                _writtenSoFar = 0;
            }

            var ret = _currentSequence;
            _currentSequence = null;
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
}
