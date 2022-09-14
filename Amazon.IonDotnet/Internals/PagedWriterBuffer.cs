/*
 * Copyright 2020 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

namespace Amazon.IonDotnet.Internals
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Text;
    using Amazon.IonDotnet.Internals.Binary;
    using Amazon.IonDotnet.Utils;

    internal abstract class PagedWriterBuffer : IWriterBuffer
    {
        protected bool isDisposed = false;

        private const int Shift1Byte = 8;
        private const int Shift2Byte = 8 * 2;
        private const int Shift3Byte = 8 * 3;
        private const int Shift4Byte = 8 * 4;
        private const int Shift5Byte = 8 * 5;
        private const int Shift6Byte = 8 * 6;
        private const int Shift7Byte = 8 * 7;

        private const int IntBitsPerOctet = 7;
        private const int VarUintUnitFlag = 0b_0111_1111; // 0x7f
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

        private const int VarIntFinalOctetMask = 0b_1000_0000; // 0x80

        private const byte VarIntSignedOctetMask = 0x3F;

        private const byte VarInt10OctetShift = 62;
        private const long VarInt10OctetMinValue = 1L << VarInt10OctetShift;
        private const long VarInt9OctetMinValue = VarUintShift8UnitMinValue >> 1;
        private const long VarInt8OctetMinValue = VarUintShift7UnitMinValue >> 1;
        private const long VarInt7OctetMinValue = VarUintShift6UnitMinValue >> 1;
        private const long VarInt6OctetMinValue = VarUintShift5UnitMinValue >> 1;
        private const long VarInt5OctetMinValue = VarUintShift4UnitMinValue >> 1;
        private const long VarInt4OctetMinValue = VarUintShift3UnitMinValue >> 1;
        private const long VarInt3OctetMinValue = VarUintShift2UnitMinValue >> 1;
        private const long VarInt2OctetMinValue = VarUintShift1UnitMinValue >> 1;

        private readonly List<byte[]> bufferBlocks;

        /// <summary>
        /// The minimum size for blocks rented from the ArrayPool.
        /// </summary>
        private readonly int intendedBlockSize;

        private byte[] currentBlock;
        private IList<Memory<byte>> currentSequence;

        /// <summary>
        /// The smallest unwritten index of the <see cref="currentBlock"/>.
        /// <para>If this equals block size, that means current block is full.</para>
        /// </summary>
        private int runningIndex;

        /// <summary>
        /// Total bytes written since the last <see cref="Wrapup"/>.
        /// </summary>
        private long writtenSoFar;

        protected PagedWriterBuffer(int intendedBlockSize)
        {
            // Less than 4 is too small
            Debug.Assert(intendedBlockSize >= 4, "intendedBlockSize is less than 4");

            this.currentBlock = ArrayPool<byte>.Shared.Rent(intendedBlockSize);
            this.bufferBlocks = new List<byte[]> { this.currentBlock };
            this.intendedBlockSize = intendedBlockSize;
        }

        ~PagedWriterBuffer()
        {
            this.Dispose(false);
        }

        public int WriteUtf8(ReadOnlySpan<char> chars, int length)
        {
            this.ThrowIfDisposed();

            // get the byteCount first
            var byteCount = length == -1 ? Encoding.UTF8.GetByteCount(chars) : length;
            Debug.Assert(length == -1 || length == Encoding.UTF8.GetByteCount(chars), "length is not -1 or matches chars ByteCount");
            Span<byte> alloc = stackalloc byte[BinaryConstants.ShortStringLength];
            if (byteCount > this.currentBlock.Length - this.runningIndex)
            {
                this.WriteCharsSlow(chars, byteCount, alloc);
                return byteCount;
            }

            Encoding.UTF8.GetBytes(chars, new Span<byte>(this.currentBlock, this.runningIndex, byteCount));
            this.runningIndex += byteCount;
            this.writtenSoFar += byteCount;
            return byteCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteByte(byte octet)
        {
            this.ThrowIfDisposed();
            if (this.runningIndex == this.currentBlock.Length)
            {
                this.AllocateNewBlock();
            }

            this.currentBlock[this.runningIndex++] = octet;
            this.writtenSoFar++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUint8(long value) => this.WriteByte((byte)value);

        public void WriteUint16(long value)
        {
            this.ThrowIfDisposed();
            if (this.currentBlock.Length - this.runningIndex < 2)
            {
                this.WriteByte((byte)(value >> Shift1Byte));
                this.WriteByte((byte)value);
                return;
            }

            this.currentBlock[this.runningIndex++] = (byte)(value >> Shift1Byte);
            this.currentBlock[this.runningIndex++] = (byte)value;
            this.writtenSoFar += 2;
        }

        public void WriteUint24(long value)
        {
            this.ThrowIfDisposed();
            if (this.currentBlock.Length - this.runningIndex < 3)
            {
                this.WriteByte((byte)(value >> Shift2Byte));
                this.WriteByte((byte)(value >> Shift1Byte));
                this.WriteByte((byte)value);
                return;
            }

            this.currentBlock[this.runningIndex++] = (byte)(value >> Shift2Byte);
            this.currentBlock[this.runningIndex++] = (byte)(value >> Shift1Byte);
            this.currentBlock[this.runningIndex++] = (byte)value;
            this.writtenSoFar += 3;
        }

        public void WriteUint32(long value)
        {
            this.ThrowIfDisposed();
            if (this.currentBlock.Length - this.runningIndex < 4)
            {
                this.WriteByte((byte)(value >> Shift3Byte));
                this.WriteByte((byte)(value >> Shift2Byte));
                this.WriteByte((byte)(value >> Shift1Byte));
                this.WriteByte((byte)value);
                return;
            }

            this.currentBlock[this.runningIndex++] = (byte)(value >> Shift3Byte);
            this.currentBlock[this.runningIndex++] = (byte)(value >> Shift2Byte);
            this.currentBlock[this.runningIndex++] = (byte)(value >> Shift1Byte);
            this.currentBlock[this.runningIndex++] = (byte)value;
            this.writtenSoFar += 4;
        }

        public void WriteUint40(long value)
        {
            this.ThrowIfDisposed();
            if (this.currentBlock.Length - this.runningIndex < 5)
            {
                this.WriteByte((byte)(value >> Shift4Byte));
                this.WriteByte((byte)(value >> Shift3Byte));
                this.WriteByte((byte)(value >> Shift2Byte));
                this.WriteByte((byte)(value >> Shift1Byte));
                this.WriteByte((byte)value);
                return;
            }

            this.currentBlock[this.runningIndex++] = (byte)(value >> Shift4Byte);
            this.currentBlock[this.runningIndex++] = (byte)(value >> Shift3Byte);
            this.currentBlock[this.runningIndex++] = (byte)(value >> Shift2Byte);
            this.currentBlock[this.runningIndex++] = (byte)(value >> Shift1Byte);
            this.currentBlock[this.runningIndex++] = (byte)value;
            this.writtenSoFar += 5;
        }

        public void WriteUint48(long value)
        {
            this.ThrowIfDisposed();
            if (this.currentBlock.Length - this.runningIndex < 6)
            {
                this.WriteByte((byte)(value >> Shift5Byte));
                this.WriteByte((byte)(value >> Shift4Byte));
                this.WriteByte((byte)(value >> Shift3Byte));
                this.WriteByte((byte)(value >> Shift2Byte));
                this.WriteByte((byte)(value >> Shift1Byte));
                this.WriteByte((byte)value);
                return;
            }

            this.currentBlock[this.runningIndex++] = (byte)(value >> Shift5Byte);
            this.currentBlock[this.runningIndex++] = (byte)(value >> Shift4Byte);
            this.currentBlock[this.runningIndex++] = (byte)(value >> Shift3Byte);
            this.currentBlock[this.runningIndex++] = (byte)(value >> Shift2Byte);
            this.currentBlock[this.runningIndex++] = (byte)(value >> Shift1Byte);
            this.currentBlock[this.runningIndex++] = (byte)value;
            this.writtenSoFar += 6;
        }

        public void WriteUint56(long value)
        {
            this.ThrowIfDisposed();
            if (this.currentBlock.Length - this.runningIndex < 7)
            {
                this.WriteByte((byte)(value >> Shift6Byte));
                this.WriteByte((byte)(value >> Shift5Byte));
                this.WriteByte((byte)(value >> Shift4Byte));
                this.WriteByte((byte)(value >> Shift3Byte));
                this.WriteByte((byte)(value >> Shift2Byte));
                this.WriteByte((byte)(value >> Shift1Byte));
                this.WriteByte((byte)value);
                return;
            }

            this.currentBlock[this.runningIndex++] = (byte)(value >> Shift6Byte);
            this.currentBlock[this.runningIndex++] = (byte)(value >> Shift5Byte);
            this.currentBlock[this.runningIndex++] = (byte)(value >> Shift4Byte);
            this.currentBlock[this.runningIndex++] = (byte)(value >> Shift3Byte);
            this.currentBlock[this.runningIndex++] = (byte)(value >> Shift2Byte);
            this.currentBlock[this.runningIndex++] = (byte)(value >> Shift1Byte);
            this.currentBlock[this.runningIndex++] = (byte)value;
            this.writtenSoFar += 7;
        }

        public void WriteUint64(long value)
        {
            this.ThrowIfDisposed();
            if (this.currentBlock.Length - this.runningIndex < 8)
            {
                this.WriteByte((byte)(value >> Shift7Byte));
                this.WriteByte((byte)(value >> Shift6Byte));
                this.WriteByte((byte)(value >> Shift5Byte));
                this.WriteByte((byte)(value >> Shift4Byte));
                this.WriteByte((byte)(value >> Shift3Byte));
                this.WriteByte((byte)(value >> Shift2Byte));
                this.WriteByte((byte)(value >> Shift1Byte));
                this.WriteByte((byte)value);
                return;
            }

            this.currentBlock[this.runningIndex++] = (byte)(value >> Shift7Byte);
            this.currentBlock[this.runningIndex++] = (byte)(value >> Shift6Byte);
            this.currentBlock[this.runningIndex++] = (byte)(value >> Shift5Byte);
            this.currentBlock[this.runningIndex++] = (byte)(value >> Shift4Byte);
            this.currentBlock[this.runningIndex++] = (byte)(value >> Shift3Byte);
            this.currentBlock[this.runningIndex++] = (byte)(value >> Shift2Byte);
            this.currentBlock[this.runningIndex++] = (byte)(value >> Shift1Byte);
            this.currentBlock[this.runningIndex++] = (byte)value;
            this.writtenSoFar += 8;
        }

        public void WriteBytes(ReadOnlySpan<byte> bytes)
        {
            this.ThrowIfDisposed();
            var bytesToWrite = bytes.Length;
            while (bytesToWrite > 0)
            {
                // first, write what we can
                var left = this.currentBlock.Length - this.runningIndex;
                var bytesWritten = bytesToWrite > left ? left : bytesToWrite;
                bytes.Slice(0, bytesWritten).CopyTo(new Span<byte>(this.currentBlock, this.runningIndex, bytesWritten));
                this.runningIndex += bytesWritten;
                this.writtenSoFar += bytesWritten;
                bytesToWrite -= bytesWritten;

                Debug.Assert(this.runningIndex <= this.currentBlock.Length, "runningIndex is greater than currentBlock size");
                if (bytesToWrite == 0)
                {
                    break;
                }

                Debug.Assert(this.runningIndex == this.currentBlock.Length, "runningIndex does not match currentBlock size");

                // new allocation needed
                this.AllocateNewBlock();
                bytes = bytes.Slice(bytesWritten);
            }
        }

        public int WriteVarUint(long value)
        {
            this.ThrowIfDisposed();
            if (value < VarUintShift1UnitMinValue)
            {
                // fits in 1 byte
                this.WriteUint8((value & VarUintUnitFlag) | VarIntFinalOctetMask);
                return 1;
            }

            if (value < VarUintShift2UnitMinValue)
            {
                return this.currentBlock.Length - this.runningIndex > 2
                    ? this.WriteVarUIntDirect2(value)
                    : this.WriteVarUIntSlow(value);
            }

            if (value < VarUintShift3UnitMinValue)
            {
                return this.currentBlock.Length - this.runningIndex > 3
                    ? this.WriteVarUIntDirect3(value)
                    : this.WriteVarUIntSlow(value);
            }

            if (value < VarUintShift4UnitMinValue)
            {
                return this.currentBlock.Length - this.runningIndex > 4
                    ? this.WriteVarUIntDirect4(value)
                    : this.WriteVarUIntSlow(value);
            }

            if (value < VarUintShift5UnitMinValue)
            {
                return this.currentBlock.Length - this.runningIndex > 5
                    ? this.WriteVarUIntDirect5(value)
                    : this.WriteVarUIntSlow(value);
            }

            return this.WriteVarUIntSlow(value);
        }

        public int WriteAnnotationsWithLength(IList<SymbolToken> annotations)
        {
            this.ThrowIfDisposed();

            // remember the current position to write the length
            // annotation length MUST fit in 1 byte
            if (this.runningIndex == this.currentBlock.Length)
            {
                this.AllocateNewBlock();
            }

            var lengthPosIdx = this.runningIndex++;
            var lengthPosBlock = this.currentBlock;
            var annotLength = 0;

            // this accounts for the tid|length byte
            this.writtenSoFar++;

            for (int i = 0, l = annotations.Count; i < l; i++)
            {
                annotLength += this.WriteVarUint(annotations[i].Sid);
                if (annotLength > BinaryConstants.MaxAnnotationSize)
                {
                    throw new IonException($"Annotation size too large: {annotLength} bytes");
                }
            }

            lengthPosBlock[lengthPosIdx] = (byte)((annotLength & VarUintUnitFlag) | VarIntFinalOctetMask);
            return annotLength + 1;
        }

        /// <summary>
        /// Write the number in the form of var-int, meaning that the last byte contains the sign bit.
        /// </summary>
        /// <param name="value">Number to write as a long.</param>
        /// <returns>Number of bytes written.</returns>
        public int WriteVarInt(long value)
        {
            this.ThrowIfDisposed();
            Debug.Assert(value != long.MinValue, "value is long.MinValue");

            const int varIntBitsPerSignedOctet = 6;
            const int varSint2OctetShift = varIntBitsPerSignedOctet + (1 * IntBitsPerOctet);
            const int varSint3OctetShift = varIntBitsPerSignedOctet + (2 * IntBitsPerOctet);
            const int varSint4OctetShift = varIntBitsPerSignedOctet + (3 * IntBitsPerOctet);
            const int varSint5OctetShift = varIntBitsPerSignedOctet + (4 * IntBitsPerOctet);

            var signMask = (byte)(value < 0 ? 0b0100_0000 : 0);
            var magnitude = value < 0 ? -value : value;
            if (magnitude < VarInt2OctetMinValue)
            {
                this.WriteUint8((magnitude & VarIntSignedOctetMask) | VarIntFinalOctetMask | signMask);
                return 1;
            }

            long signBit = value < 0 ? 1 : 0;
            var remaining = this.currentBlock.Length - this.runningIndex;

            if (magnitude < VarInt3OctetMinValue && remaining >= 2)
            {
                return this.WriteVarUIntDirect2(magnitude | (signBit << varSint2OctetShift));
            }

            if (magnitude < VarInt4OctetMinValue && remaining >= 3)
            {
                return this.WriteVarUIntDirect3(magnitude | (signBit << varSint3OctetShift));
            }

            if (magnitude < VarInt5OctetMinValue && remaining >= 4)
            {
                return this.WriteVarUIntDirect4(magnitude | (signBit << varSint4OctetShift));
            }

            if (magnitude < VarInt6OctetMinValue && remaining >= 5)
            {
                return this.WriteVarUIntDirect5(magnitude | (signBit << varSint5OctetShift));
            }

            return this.WriteVarIntSlow(magnitude, signMask);
        }

        public void StartStreak(IList<Memory<byte>> sequence)
        {
            this.ThrowIfDisposed();
            this.currentSequence = sequence;
            if (this.currentBlock == null)
            {
                this.AllocateNewBlock();
            }
        }

        public IList<Memory<byte>> Wrapup()
        {
            this.ThrowIfDisposed();
            Debug.Assert(this.currentSequence != null, "currentSequence is null");

            if (this.writtenSoFar == 0)
            {
                return this.currentSequence;
            }

            if (this.runningIndex >= this.writtenSoFar)
            {
                // this means that all the bytes written since the last wrapup() fits in one block
                // so just need to return that segment
                // make sure that we are conservative in array here
                if (this.runningIndex > 0)
                {
                    this.currentSequence.Add(new Memory<byte>(this.currentBlock, this.runningIndex - (int)this.writtenSoFar, (int)this.writtenSoFar));
                }

                this.writtenSoFar = 0;
            }
            else
            {
                // this means we have reached a new block since last wrapup(), and all previous segments have been added
                // we just need to add the current segment
                this.currentSequence.Add(new Memory<byte>(this.currentBlock, 0, this.runningIndex));
                this.writtenSoFar = 0;
            }

            return this.currentSequence;
        }

        public void Reset()
        {
            this.ThrowIfDisposed();
            this.currentBlock = null;
            this.writtenSoFar = 0;
            this.runningIndex = 0;
            this.currentSequence = null;

            // keep 1 block
            while (this.bufferBlocks.Count > 1)
            {
                var idx = this.bufferBlocks.Count - 1;
                ArrayPool<byte>.Shared.Return(this.bufferBlocks[idx]);
                this.bufferBlocks.RemoveAt(idx);
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.isDisposed)
            {
                return;
            }
            else if (disposing)
            {
                this.isDisposed = true;
                this.currentBlock = null;
                foreach (var block in this.bufferBlocks)
                {
                    ArrayPool<byte>.Shared.Return(block);
                }

                this.bufferBlocks.Clear();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte TransformVarIntOctet(byte b, int size, byte signMask)
        {
            if (size == 1)
            {
                b &= VarIntSignedOctetMask;
                b |= signMask;
            }
            else
            {
                b &= VarUintUnitFlag;
            }

            return b;
        }

        private void ThrowIfDisposed()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException("PagedWriterBuffer");
            }
        }

        /// <summary>
        /// The slow path, write with allocations.
        /// </summary>
        /// <param name="chars">Char sequence.</param>
        /// <param name="bytesToWrite">Total bytes needed to utf8-encode the string.</param>
        /// <param name="alloc">The allocation.</param>
        private void WriteCharsSlow(ReadOnlySpan<char> chars, int bytesToWrite, Span<byte> alloc)
        {
            if (bytesToWrite <= BinaryConstants.ShortStringLength)
            {
                this.WriteShortChars(chars, bytesToWrite, alloc);
            }
            else
            {
                this.WriteLongChars(chars, bytesToWrite);
            }
        }

        private void WriteShortChars(ReadOnlySpan<char> chars, int bytesToWrite, Span<byte> alloc)
        {
            var length = Encoding.UTF8.GetBytes(chars, alloc);
            Debug.Assert(length == bytesToWrite, "length does not equal bytesToWrite");
            this.WriteBytes(alloc.Slice(0, bytesToWrite));
        }

        private void WriteLongChars(ReadOnlySpan<char> chars, int bytesToWrite)
        {
            Span<byte> alloc = new byte[bytesToWrite];
            Encoding.UTF8.GetBytes(chars, alloc);
            this.WriteBytes(alloc);
        }

        /// <summary>
        /// Allocate new memory block and update related fields.
        /// </summary>
        private void AllocateNewBlock()
        {
            // First we gotta add the end segment to the list of current segment sequence
            Debug.Assert(this.currentSequence != null, "currentSequence is null");
            if (this.runningIndex < this.writtenSoFar)
            {
                // writtenSoFar > BlockSize means this whole block is to be added
                this.currentSequence.Add(this.currentBlock);
            }
            else if (this.runningIndex > 0)
            {
                // this means that all the bytes written since the last wrapup() fits in one block
                this.currentSequence.Add(new Memory<byte>(this.currentBlock, this.runningIndex - (int)this.writtenSoFar, (int)this.writtenSoFar));
            }

            this.runningIndex = 0;
            if (this.currentBlock == null && this.bufferBlocks.Count > 0)
            {
                this.currentBlock = this.bufferBlocks[0];
                return;
            }

            var newBlock = ArrayPool<byte>.Shared.Rent(this.intendedBlockSize);
            this.bufferBlocks.Add(newBlock);
            this.currentBlock = newBlock;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int WriteVarUIntDirect2(long value)
        {
            this.currentBlock[this.runningIndex++] = (byte)((value >> VarUintShift1Unit) & VarUintUnitFlag);
            this.currentBlock[this.runningIndex++] = (byte)((value & VarUintUnitFlag) | VarIntFinalOctetMask);
            this.writtenSoFar += 2;
            return 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int WriteVarUIntDirect3(long value)
        {
            this.currentBlock[this.runningIndex++] = (byte)((value >> VarUintShift2Unit) & VarUintUnitFlag);
            this.currentBlock[this.runningIndex++] = (byte)((value >> VarUintShift1Unit) & VarUintUnitFlag);
            this.currentBlock[this.runningIndex++] = (byte)((value & VarUintUnitFlag) | VarIntFinalOctetMask);
            this.writtenSoFar += 3;
            return 3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int WriteVarUIntDirect4(long value)
        {
            this.currentBlock[this.runningIndex++] = (byte)((value >> VarUintShift3Unit) & VarUintUnitFlag);
            this.currentBlock[this.runningIndex++] = (byte)((value >> VarUintShift2Unit) & VarUintUnitFlag);
            this.currentBlock[this.runningIndex++] = (byte)((value >> VarUintShift1Unit) & VarUintUnitFlag);
            this.currentBlock[this.runningIndex++] = (byte)((value & VarUintUnitFlag) | VarIntFinalOctetMask);
            this.writtenSoFar += 4;
            return 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int WriteVarUIntDirect5(long value)
        {
            this.currentBlock[this.runningIndex++] = (byte)((value >> VarUintShift4Unit) & VarUintUnitFlag);
            this.currentBlock[this.runningIndex++] = (byte)((value >> VarUintShift3Unit) & VarUintUnitFlag);
            this.currentBlock[this.runningIndex++] = (byte)((value >> VarUintShift2Unit) & VarUintUnitFlag);
            this.currentBlock[this.runningIndex++] = (byte)((value >> VarUintShift1Unit) & VarUintUnitFlag);
            this.currentBlock[this.runningIndex++] = (byte)((value & VarUintUnitFlag) | VarIntFinalOctetMask);
            this.writtenSoFar += 5;
            return 5;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int WriteVarUIntSlow(long value)
        {
            var size = 1;
            if (value >= VarUintShift8UnitMinValue)
            {
                this.WriteUint8((value >> VarUintShift8Unit) & VarUintUnitFlag);
                size++;
            }

            if (value >= VarUintShift7UnitMinValue)
            {
                this.WriteUint8((value >> VarUintShift7Unit) & VarUintUnitFlag);
                size++;
            }

            if (value >= VarUintShift6UnitMinValue)
            {
                this.WriteUint8((value >> VarUintShift6Unit) & VarUintUnitFlag);
                size++;
            }

            if (value >= VarUintShift5UnitMinValue)
            {
                this.WriteUint8((value >> VarUintShift5Unit) & VarUintUnitFlag);
                size++;
            }

            if (value >= VarUintShift4UnitMinValue)
            {
                this.WriteUint8((value >> VarUintShift4Unit) & VarUintUnitFlag);
                size++;
            }

            if (value >= VarUintShift3UnitMinValue)
            {
                this.WriteUint8((value >> VarUintShift3Unit) & VarUintUnitFlag);
                size++;
            }

            if (value >= VarUintShift2UnitMinValue)
            {
                this.WriteUint8((value >> VarUintShift2Unit) & VarUintUnitFlag);
                size++;
            }

            if (value >= VarUintShift1UnitMinValue)
            {
                this.WriteUint8((value >> VarUintShift1Unit) & VarUintUnitFlag);
                size++;
            }

            this.WriteUint8((value & VarUintUnitFlag) | VarIntFinalOctetMask);
            return size;
        }

        private int WriteVarIntSlow(long magnitude, byte signMask)
        {
            var size = 1;
            byte b;
            if (magnitude >= VarInt10OctetMinValue)
            {
                b = (byte)(magnitude >> VarInt10OctetShift);
                b &= VarIntSignedOctetMask;
                b |= signMask;
                this.WriteByte(b);
                size++;
            }

            if (magnitude >= VarInt9OctetMinValue)
            {
                b = (byte)(magnitude >> VarUintShift8Unit);
                b = TransformVarIntOctet(b, size, signMask);

                this.WriteByte(b);
                size++;
            }

            if (magnitude >= VarInt8OctetMinValue)
            {
                b = (byte)(magnitude >> VarUintShift7Unit);
                b = TransformVarIntOctet(b, size, signMask);

                this.WriteByte(b);
                size++;
            }

            if (magnitude >= VarInt7OctetMinValue)
            {
                b = (byte)(magnitude >> VarUintShift6Unit);
                b = TransformVarIntOctet(b, size, signMask);

                this.WriteByte(b);
                size++;
            }

            if (magnitude >= VarInt6OctetMinValue)
            {
                b = (byte)(magnitude >> VarUintShift5Unit);
                b = TransformVarIntOctet(b, size, signMask);

                this.WriteByte(b);
                size++;
            }

            if (magnitude >= VarInt5OctetMinValue)
            {
                b = (byte)(magnitude >> VarUintShift4Unit);
                b = TransformVarIntOctet(b, size, signMask);

                this.WriteByte(b);
                size++;
            }

            if (magnitude >= VarInt4OctetMinValue)
            {
                b = (byte)(magnitude >> VarUintShift3Unit);
                b = TransformVarIntOctet(b, size, signMask);

                this.WriteByte(b);
                size++;
            }

            if (magnitude >= VarInt3OctetMinValue)
            {
                b = (byte)(magnitude >> VarUintShift2Unit);
                b = TransformVarIntOctet(b, size, signMask);

                this.WriteByte(b);
                size++;
            }

            if (magnitude >= VarInt2OctetMinValue)
            {
                b = (byte)(magnitude >> VarUintShift1Unit);
                b = TransformVarIntOctet(b, size, signMask);

                this.WriteByte(b);
                size++;
            }

            b = (byte)magnitude;
            b = TransformVarIntOctet(b, size, signMask);

            b |= VarIntFinalOctetMask;
            this.WriteByte(b);
            return size;
        }
    }
}
