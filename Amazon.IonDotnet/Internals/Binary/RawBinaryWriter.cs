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

namespace Amazon.IonDotnet.Internals.Binary
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading.Tasks;
    using Amazon.IonDotnet.Utils;

#if !(NETSTANDARD2_0 || NET45)
    using BitConverterEx = System.BitConverter;
#endif

    internal class RawBinaryWriter : IPrivateWriter
    {
        internal readonly List<SymbolToken> Annotations = new List<SymbolToken>();

        private const int IntZeroByte = 0x20;

        // High-bits of different value types
        private const byte TidPosIntByte = 0x20;
        private const byte TidNegIntByte = 0x30;
        private const byte TidListByte = 0xB0;
        private const byte TidSexpByte = 0xC0;
        private const byte TidStructByte = 0xD0;
        private const byte TidTypeDeclByte = 0xE0;
        private const byte TidStringByte = 0x80;
        private const byte TidClobType = 0x90;
        private const byte TidBlobByte = 0xA0;
        private const byte TidFloatByte = 0x40;
        private const byte TidDecimalByte = 0x50;
        private const byte TidTimestampByte = 0x60;
        private const byte TidSymbolType = 0x70;

        private const byte BoolFalseByte = 0x10;
        private const byte BoolTrueByte = 0x11;

        private const int DefaultContainerStackSize = 6;

        private readonly IWriterBuffer lengthBuffer;
        private readonly IWriterBuffer dataBuffer;

        private readonly ContainerStack containerStack;
        private readonly List<Memory<byte>> lengthSegments;
        private readonly bool forceFloat64;

        private SymbolToken currentFieldSymbolToken;

        internal RawBinaryWriter(IWriterBuffer lengthBuffer, IWriterBuffer dataBuffer, List<Memory<byte>> lengthSegments, bool forceFloat64)
        {
            this.lengthBuffer = lengthBuffer;
            this.dataBuffer = dataBuffer;
            this.lengthSegments = lengthSegments;
            this.containerStack = new ContainerStack(DefaultContainerStackSize);
            this.forceFloat64 = forceFloat64;

            // Top-level writing also requires a tracker
            var pushedContainer = this.containerStack.PushContainer(ContainerType.Datagram);
            this.dataBuffer.StartStreak(pushedContainer.Sequence);
        }

        private enum ContainerType
        {
            List,
            Sexp,
            Struct,
            Annotation,
            Datagram,

            // To be used in the case where a value is treated as a container
            Timestamp,
            BigDecimal,
        }

        public ISymbolTable SymbolTable => SharedSymbolTable.GetSystem(1);

        public bool IsInStruct => this.containerStack.Count > 0 && this.containerStack.Peek().Type == ContainerType.Struct;

        /// <summary>
        /// Simply write the buffers (async), <see cref="PrepareFlush"/> should be called first.
        /// </summary>
        /// <param name="outputStream">Stream to flush to.</param>
        /// <returns>Task for FlushAsync.</returns>
        public async Task FlushAsync(Stream outputStream)
        {
            Debug.Assert(this.containerStack.Count == 1, $"{this.containerStack.Count}");
            Debug.Assert(outputStream?.CanWrite == true, "CanWrite is false");
            var currentSequence = this.containerStack.Peek().Sequence;

            // Now write
            foreach (var segment in currentSequence)
            {
                await outputStream.WriteAsync(segment);
            }

            outputStream.Flush();
        }

        /// <summary>
        /// Simply write the buffers (blocking), <see cref="PrepareFlush"/> should be called first.
        /// </summary>
        /// <param name="outputStream">Stream to flush to.</param>
        public void Flush(Stream outputStream)
        {
            Debug.Assert(this.containerStack.Count == 1, $"{this.containerStack.Count}");
            Debug.Assert(outputStream?.CanWrite == true, "CanWrite is false");
            var currentSequence = this.containerStack.Peek().Sequence;

            // Now write
            foreach (var segment in currentSequence)
            {
                outputStream.Write(segment.Span);
            }

            outputStream.Flush();
        }

        void IIonWriter.Flush()
        {
        }

        public void Finish()
        {
            // TODO: Implement writing again after finish
        }

        public void SetFieldName(string name) => throw new NotSupportedException("Cannot set a field name here");

        public void SetFieldNameSymbol(SymbolToken symbol)
        {
            if (!this.IsInStruct)
            {
                throw new IonException("Has to be in a struct to set a field name");
            }

            this.currentFieldSymbolToken = symbol;
        }

        public void StepIn(IonType type)
        {
            if (!type.IsContainer())
            {
                throw new IonException($"Cannot step into {type}");
            }

            this.PrepareValue();

            // Wrapup the current writes
            if (this.containerStack.Count > 0)
            {
                var writeList = this.dataBuffer.Wrapup();
                Debug.Assert(ReferenceEquals(writeList, this.containerStack.Peek().Sequence), "writeList does not equal Sequence");
            }

            var pushedContainer = this.containerStack.PushContainer(this.GetContainerType(type));
            this.dataBuffer.StartStreak(pushedContainer.Sequence);
        }

        public void StepOut()
        {
            if (this.currentFieldSymbolToken != default)
            {
                throw new IonException("Cannot step out with field name set");
            }

            if (this.Annotations.Count > 0)
            {
                throw new IonException("Cannot step out with Annotations set");
            }

            // TODO: Check if this container is actually list or struct
            var currentContainerType = this.containerStack.Peek().Type;

            if (this.IsNotContainerType(currentContainerType))
            {
                throw new IonException($"Cannot step out of {currentContainerType}");
            }

            this.PopContainer();

            // Clear annotations
            this.FinishValue();
        }

        void IIonWriter.WriteValue(IIonReader reader) => throw new NotSupportedException();

        void IIonWriter.WriteValues(IIonReader reader) => throw new NotSupportedException();

        void IIonWriter.SetTypeAnnotations(IEnumerable<string> annotations) => throw new NotSupportedException();

        public void WriteNull()
        {
            const byte nullNull = 0x0F;
            this.PrepareValue();
            this.containerStack.IncreaseCurrentContainerLength(1);
            this.dataBuffer.WriteByte(nullNull);
            this.FinishValue();
        }

        public void WriteNull(IonType type)
        {
            var nullByte = BinaryConstants.GetNullByte(type);
            this.PrepareValue();
            this.containerStack.IncreaseCurrentContainerLength(1);
            this.dataBuffer.WriteByte(nullByte);
            this.FinishValue();
        }

        public void WriteBool(bool value)
        {
            this.PrepareValue();
            this.containerStack.IncreaseCurrentContainerLength(1);
            this.dataBuffer.WriteByte(value ? BoolTrueByte : BoolFalseByte);
        }

        public void WriteInt(long value)
        {
            this.PrepareValue();
            if (value == 0)
            {
                this.containerStack.IncreaseCurrentContainerLength(1);
                this.dataBuffer.WriteByte(IntZeroByte);
            }
            else if (value < 0)
            {
                if (value == long.MinValue)
                {
                    // XXX special case for min_value which will not play nice with signed
                    // arithmetic and fit into the positive space
                    // XXX we keep 2's complement of Long.MIN_VALUE because it encodes to unsigned 2
                    // ** 63 (0x8000000000000000L)
                    // XXX WriteBuffer.writeUInt64() never looks at sign
                    this.dataBuffer.WriteByte(TidNegIntByte | 0x8);
                    this.dataBuffer.WriteUint64(value);
                    this.containerStack.IncreaseCurrentContainerLength(9);
                }
                else
                {
                    this.WriteTypedUInt(TidNegIntByte, -value);
                }
            }
            else
            {
                this.WriteTypedUInt(TidPosIntByte, value);
            }

            this.FinishValue();
        }

        public void WriteInt(BigInteger value)
        {
            if (value >= long.MinValue && value <= long.MaxValue)
            {
                this.WriteInt((long)value);
                return;
            }

            this.PrepareValue();

            var type = TidPosIntByte;
            if (value < 0)
            {
                type = TidNegIntByte;
                value = BigInteger.Negate(value);
            }

            // TODO: Is there a no-alloc way?
#if NET45 || NETSTANDARD2_0
            var buffer = value.ToByteArray();
            Array.Reverse(buffer);
#else
            var buffer = value.ToByteArray(isBigEndian: true);
#endif
            this.WriteTypedBytes(type, buffer);

            this.FinishValue();
        }

        public void WriteFloat(double value)
        {
            this.PrepareValue();

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (!this.forceFloat64 && value == (float)value)
            {
                // TODO: Increase testing coverage
                this.containerStack.IncreaseCurrentContainerLength(5);
                this.dataBuffer.WriteByte(TidFloatByte | 4);
                this.dataBuffer.WriteUint32(BitConverterEx.SingleToInt32Bits((float)value));
            }
            else
            {
                this.containerStack.IncreaseCurrentContainerLength(9);
                this.dataBuffer.WriteByte(TidFloatByte | 8);

                if (double.IsNaN(value))
                {
                    // Double.NaN is different between C# and Java
                    // For consistency, map NaN to the long value for NaN in Java
                    this.dataBuffer.WriteUint64(0x7ff8000000000000L);
                }
                else
                {
                    this.dataBuffer.WriteUint64(BitConverter.DoubleToInt64Bits(value));
                }
            }

            this.FinishValue();
        }

        public void WriteDecimal(decimal value)
        {
            this.PrepareValue();

            if (value == 0)
            {
                this.containerStack.IncreaseCurrentContainerLength(1);
                this.dataBuffer.WriteUint8(TidDecimalByte);
            }
            else
            {
                this.WriteDecimalNumber(value, true);
            }

            this.FinishValue();
        }

        public void WriteDecimal(BigDecimal value)
        {
            if (value.IntVal == 0 && value.Scale == 0 && !value.IsNegativeZero)
            {
                this.WriteDecimal(value.ToDecimal());
                return;
            }

            this.PrepareValue();

            // Wrapup first
            // Add all written segments to the sequence
            this.dataBuffer.Wrapup();

            // Set a new container
            var newContainer = this.containerStack.PushContainer(ContainerType.BigDecimal);

            this.dataBuffer.StartStreak(newContainer.Sequence);
            var totalLength = this.dataBuffer.WriteVarInt(-value.Scale);
            var negative = value.IntVal < 0 || value.IsNegativeZero;
            var mag = BigInteger.Abs(value.IntVal);

#if NET45 || NETSTANDARD2_0
            var bytes = mag.ToByteArray();
            Array.Reverse(bytes);
#else
            var bytes = mag.ToByteArray(isBigEndian: true);
#endif

            if (negative)
            {
                if ((bytes[0] & 0b1000_0000) == 0)
                {
                    // bytes[0] can store the sign bit
                    bytes[0] |= 0b1000_0000;
                }
                else
                {
                    // Use an extra sign byte
                    totalLength++;
                    this.dataBuffer.WriteUint8(0b1000_0000);
                }
            }
            else if (mag.IsZero)
            {
                bytes = new byte[] { };
            }

            totalLength += bytes.Length;
            this.dataBuffer.WriteBytes(bytes);
            this.containerStack.IncreaseCurrentContainerLength(totalLength);

            // Finish up
            this.PopContainer();
            this.FinishValue();
        }

        public void WriteTimestamp(Timestamp value)
        {
            const byte varintNegZero = 0xC0;

            DateTime dateTimeValue = value.DateTimeValue.Kind == DateTimeKind.Local
                ? value.DateTimeValue.AddMinutes(-value.LocalOffset)
                : value.DateTimeValue;

            this.PrepareValue();

            // Wrapup first
            // Add all written segments to the sequence
            this.dataBuffer.Wrapup();

            // Set a new container
            var newContainer = this.containerStack.PushContainer(ContainerType.Timestamp);
            var totalLength = 0;
            this.dataBuffer.StartStreak(newContainer.Sequence);
            if (value.DateTimeValue.Kind == DateTimeKind.Unspecified)
            {
                // Unknown offset
                totalLength++;
                this.dataBuffer.WriteByte(varintNegZero);
            }
            else
            {
                totalLength += this.dataBuffer.WriteVarInt(value.LocalOffset);
            }

            this.containerStack.IncreaseCurrentContainerLength(totalLength);

            // Don't update totallength here.
            this.WriteVarUint(dateTimeValue.Year);
            if (value.TimestampPrecision >= Timestamp.Precision.Month)
            {
                this.WriteVarUint(dateTimeValue.Month);
            }

            if (value.TimestampPrecision >= Timestamp.Precision.Day)
            {
                this.WriteVarUint(dateTimeValue.Day);
            }

            // The hour and minute is considered as a single component.
            if (value.TimestampPrecision >= Timestamp.Precision.Minute)
            {
                this.WriteVarUint(dateTimeValue.Hour);
                this.WriteVarUint(dateTimeValue.Minute);
            }

            if (value.TimestampPrecision >= Timestamp.Precision.Second)
            {
                this.WriteVarUint(dateTimeValue.Second);
            }

            if (value.TimestampPrecision >= Timestamp.Precision.Second && !value.FractionalSecond.ToString().Equals("0"))
            {
                this.WriteDecimalNumber(value.FractionalSecond, false);
            }

            this.PopContainer();

            this.FinishValue();
        }

        public void WriteSymbol(string symbol)
            => throw new UnsupportedIonVersionException($"Writing text symbol is not supported at raw level");

        public void WriteSymbolToken(SymbolToken token)
        {
            if (token == default)
            {
                this.WriteNull(IonType.Symbol);
                return;
            }

            Debug.Assert(token.Sid >= 0, "Sid is greater than 0");
            this.PrepareValue();
            this.WriteTypedUInt(TidSymbolType, token.Sid);
            this.FinishValue();
        }

        public void WriteString(string value)
        {
            if (value == null)
            {
                this.WriteNull(IonType.String);
                return;
            }

            this.PrepareValue();

            var stringByteSize = Encoding.UTF8.GetByteCount(value);

            // Since we know the length of the string upfront, we can just write the length right here
            var tidByte = TidStringByte;
            var totalSize = stringByteSize;
            if (stringByteSize <= 0x0D)
            {
                tidByte |= (byte)stringByteSize;
                this.dataBuffer.WriteByte(tidByte);
                totalSize += 1;
            }
            else
            {
                tidByte |= BinaryConstants.LnIsVarLen;
                this.dataBuffer.WriteByte(tidByte);
                totalSize += 1 + this.dataBuffer.WriteVarUint(stringByteSize);
            }

            this.dataBuffer.WriteUtf8(value.AsSpan(), stringByteSize);
            this.containerStack.IncreaseCurrentContainerLength(totalSize);

            this.FinishValue();
        }

        public void WriteBlob(ReadOnlySpan<byte> value)
        {
            this.PrepareValue();
            this.WriteTypedBytes(TidBlobByte, value);
            this.FinishValue();
        }

        public void WriteClob(ReadOnlySpan<byte> value)
        {
            this.PrepareValue();
            this.WriteTypedBytes(TidClobType, value);
            this.FinishValue();
        }

        public void AddTypeAnnotationSymbol(SymbolToken annotation) => this.Annotations.Add(annotation);

        public void ClearTypeAnnotations() => this.Annotations.Clear();

        public void AddTypeAnnotation(string annotation) => throw new NotSupportedException("raw writer does not support adding Annotations");

        public void Dispose()
        {
            this.dataBuffer.Dispose();
        }

        public bool IsFieldNameSet() => this.currentFieldSymbolToken != default;

        public int GetDepth() => this.containerStack.Count - 1;

        public void WriteIonVersionMarker()
        {
            this.dataBuffer.WriteUint32(0xE0_01_00_EA);
            this.containerStack.IncreaseCurrentContainerLength(4);
        }

        /// <summary>
        /// This will stage the remaining writes in the buffer to be flushed, should be called before 'Flush()'.
        /// </summary>
        /// <returns>Total size of the bytes to be flushed.</returns>
        internal int PrepareFlush()
        {
            var topContainer = this.containerStack.Peek();

            // Wrapup to append all data to the sequence,
            // but first, remember the previous position so we can update the length
            var currIdx = topContainer.Sequence.Count;
            this.dataBuffer.Wrapup();
            var increased = 0;
            for (var i = currIdx; i < topContainer.Sequence.Count; i++)
            {
                increased += topContainer.Sequence[i].Length;
            }

            this.containerStack.IncreaseCurrentContainerLength(increased);
            return (int)topContainer.Length;
        }

        internal void Reset()
        {
            // Reset the states
            this.dataBuffer.Reset();

            // Double calls to Reset() should be fine
            this.lengthBuffer.Reset();
            this.containerStack.Clear();

            // Set the top-level container
            var pushedContainer = this.containerStack.PushContainer(ContainerType.Datagram);
            this.dataBuffer.StartStreak(pushedContainer.Sequence);
        }

        internal IWriterBuffer GetLengthBuffer() => this.lengthBuffer;

        internal IWriterBuffer GetDataBuffer() => this.dataBuffer;

        /// <summary>
        /// Prepare the field name and annotations (if any).
        /// </summary>
        /// <remarks>This method should handle being called multiple times and remain in the correct state.</remarks>
        private void PrepareValue()
        {
            if (this.IsInStruct && this.currentFieldSymbolToken == default)
            {
                throw new InvalidOperationException("In a struct but field name is not set");
            }

            if (this.currentFieldSymbolToken != default)
            {
                // Write field name id
                this.WriteVarUint(this.currentFieldSymbolToken.Sid);
                this.currentFieldSymbolToken = default;
            }

            if (this.Annotations.Count > 0)
            {
                // Since annotations 'wraps' the actual value, we don't know the length,
                // (the upcoming value might be another container)
                // so we treat this as another container of type 'annotation'

                // Add all written segments to the sequence
                this.dataBuffer.Wrapup();

                // Set a new container
                var newContainer = this.containerStack.PushContainer(ContainerType.Annotation);
                this.dataBuffer.StartStreak(newContainer.Sequence);

                var annotLength = this.dataBuffer.WriteAnnotationsWithLength(this.Annotations);
                this.containerStack.IncreaseCurrentContainerLength(annotLength);

                this.Annotations.Clear();
            }
        }

        /// <summary>
        /// This is called after the value is written, and will check if the written value is wrapped within annotations.
        /// </summary>
        private void FinishValue()
        {
            if (this.containerStack.Count > 0)
            {
                var containerInfo = this.containerStack.Peek();
                if (containerInfo.Type == ContainerType.Annotation)
                {
                    this.PopContainer();
                }
            }
        }

        /// <summary>
        /// Pop a container from the container stack and link the previous container sequence with the length
        /// and sequence of the popped container.
        /// </summary>
        private void PopContainer()
        {
            var popped = this.containerStack.Pop();
            if (this.containerStack.Count == 0)
            {
                return;
            }

            var wrappedList = this.dataBuffer.Wrapup();
            Debug.Assert(ReferenceEquals(wrappedList, popped.Sequence), "wrappedList does not equal popped.Sequence");

            var outer = this.containerStack.Peek();

            // Write the tid|len byte and (maybe) the length into the length buffer
            this.lengthSegments.Clear();
            this.lengthBuffer.StartStreak(this.lengthSegments);
            byte tidByte;
            switch (popped.Type)
            {
                case ContainerType.List:
                    tidByte = TidListByte;
                    break;
                case ContainerType.Sexp:
                    tidByte = TidSexpByte;
                    break;
                case ContainerType.Struct:
                    tidByte = TidStructByte;
                    break;
                case ContainerType.Annotation:
                    tidByte = TidTypeDeclByte;
                    break;
                case ContainerType.Timestamp:
                    tidByte = TidTimestampByte;
                    break;
                case ContainerType.BigDecimal:
                    tidByte = TidDecimalByte;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var wholeContainerLength = popped.Length;
            if (wholeContainerLength <= 0xD)
            {
                // Fit in the tid byte
                tidByte |= (byte)wholeContainerLength;
                this.containerStack.IncreaseCurrentContainerLength(1 + wholeContainerLength);
                this.lengthBuffer.WriteByte(tidByte);
            }
            else
            {
                tidByte |= BinaryConstants.LnIsVarLen;
                this.lengthBuffer.WriteByte(tidByte);
                var lengthBytes = this.lengthBuffer.WriteVarUint(popped.Length);
                this.containerStack.IncreaseCurrentContainerLength(1 + lengthBytes + wholeContainerLength);
            }

            this.lengthBuffer.Wrapup();
            foreach (var t in this.lengthSegments)
            {
                outer.Sequence.Add(t);
            }

            outer.Sequence.AddRange(wrappedList);
            this.dataBuffer.StartStreak(outer.Sequence);
        }

        private void WriteVarUint(long value)
        {
            Debug.Assert(value >= 0, "value is greater than 0");
            var written = this.dataBuffer.WriteVarUint(value);
            this.containerStack.IncreaseCurrentContainerLength(written);
        }

        private void WriteTypedUInt(byte type, long value)
        {
            if (value <= 0xFFL)
            {
                this.containerStack.IncreaseCurrentContainerLength(2);
                this.dataBuffer.WriteUint8(type | 0x01);
                this.dataBuffer.WriteUint8(value);
            }
            else if (value <= 0xFFFFL)
            {
                this.containerStack.IncreaseCurrentContainerLength(3);
                this.dataBuffer.WriteUint8(type | 0x02);
                this.dataBuffer.WriteUint16(value);
            }
            else if (value <= 0xFFFFFFL)
            {
                this.containerStack.IncreaseCurrentContainerLength(4);
                this.dataBuffer.WriteUint8(type | 0x03);
                this.dataBuffer.WriteUint24(value);
            }
            else if (value <= 0xFFFFFFFFL)
            {
                this.containerStack.IncreaseCurrentContainerLength(5);
                this.dataBuffer.WriteUint8(type | 0x04);
                this.dataBuffer.WriteUint32(value);
            }
            else if (value <= 0xFFFFFFFFFFL)
            {
                this.containerStack.IncreaseCurrentContainerLength(6);
                this.dataBuffer.WriteUint8(type | 0x05);
                this.dataBuffer.WriteUint40(value);
            }
            else if (value <= 0xFFFFFFFFFFFFL)
            {
                this.containerStack.IncreaseCurrentContainerLength(7);
                this.dataBuffer.WriteUint8(type | 0x06);
                this.dataBuffer.WriteUint48(value);
            }
            else if (value <= 0xFFFFFFFFFFFFFFL)
            {
                this.containerStack.IncreaseCurrentContainerLength(8);
                this.dataBuffer.WriteUint8(type | 0x07);
                this.dataBuffer.WriteUint56(value);
            }
            else
            {
                this.containerStack.IncreaseCurrentContainerLength(9);
                this.dataBuffer.WriteUint8(type | 0x08);
                this.dataBuffer.WriteUint64(value);
            }
        }

        /// <summary>
        /// Write raw bytes with a type.
        /// </summary>
        /// <remarks>This does not do <see cref="PrepareValue"/>.</remarks> or <see cref="FinishValue"/>
        private void WriteTypedBytes(byte type, ReadOnlySpan<byte> data)
        {
            var totalLength = 1;
            if (data.Length <= 0xD)
            {
                this.dataBuffer.WriteUint8(type | (byte)data.Length);
            }
            else
            {
                this.dataBuffer.WriteUint8(type | BinaryConstants.LnIsVarLen);
                totalLength += this.dataBuffer.WriteVarUint(data.Length);
            }

            this.dataBuffer.WriteBytes(data);
            this.containerStack.IncreaseCurrentContainerLength(totalLength + data.Length);
        }

        private void WriteDecimalNumber(decimal value, bool writeTid)
        {
            Span<byte> bytes = stackalloc byte[sizeof(decimal)];
            var maxIdx = DecimalHelper.CopyDecimalBigEndian(bytes, value);

            var negative = value < 0;
            Debug.Assert(!negative || (bytes[0] & 0b1000_0000) <= 0, "Value is <= 0 but has positive flag");
            Debug.Assert(negative || (bytes[0] ^ 0b1000_0000) > 0, "Value is > 0 but has negative flag");

            var totalLength = maxIdx - 2;
            var needExtraByte = (bytes[4] & 0b_1000_0000) > 0;
            if (needExtraByte)
            {
                totalLength++;
            }

            if (writeTid)
            {
                var tidByte = TidDecimalByte;
                if (totalLength <= 0x0D)
                {
                    tidByte |= (byte)totalLength;
                    this.dataBuffer.WriteByte(tidByte);
                    totalLength++;
                }
                else
                {
                    tidByte |= BinaryConstants.LnIsVarLen;
                    this.dataBuffer.WriteByte(tidByte);
                    totalLength += 1 + this.dataBuffer.WriteVarUint(totalLength);
                }
            }

            const byte isNegativeAndDone = 0b_1100_0000;

            // byte[2] is enough to store the 28 decimal places (255>28)
            this.dataBuffer.WriteByte((byte)(bytes[2] | isNegativeAndDone));

            // Write the 'sign' byte
            if (needExtraByte)
            {
                this.dataBuffer.WriteByte((byte)(negative ? 0b_1000_0000 : 0b_0000_000));
            }
            else if (negative)
            {
                bytes[4] |= 0b_1000_0000;
            }

            this.dataBuffer.WriteBytes(bytes.Slice(4, maxIdx - 3));

            this.containerStack.IncreaseCurrentContainerLength(totalLength);
        }

        private ContainerType GetContainerType(IonType ionType)
        {
            switch (ionType)
            {
                case IonType.List:
                    return ContainerType.List;
                case IonType.Struct:
                    return ContainerType.Struct;
                case IonType.Sexp:
                    return ContainerType.Sexp;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private bool IsNotContainerType(ContainerType currentContainerType)
        {
            return currentContainerType != ContainerType.List
                && currentContainerType != ContainerType.Sexp
                && currentContainerType != ContainerType.Struct;
        }

        private class ContainerInfo
        {
            public List<Memory<byte>> Sequence;
            public ContainerType Type;
            public long Length;
        }

        private class ContainerStack
        {
            private ContainerInfo[] array;

            public ContainerStack(int initialCapacity)
            {
                Debug.Assert(initialCapacity > 0, "initialCapacity > 0");
                this.array = new ContainerInfo[initialCapacity];
            }

            public int Count { get; private set; }

            public ContainerInfo PushContainer(ContainerType containerType)
            {
                this.EnsureCapacity(this.Count);
                if (this.array[this.Count] == null)
                {
                    this.array[this.Count] = new ContainerInfo
                    {
                        Sequence = new List<Memory<byte>>(2),
                        Type = containerType,
                    };
                    return this.array[this.Count++];
                }

                var entry = this.array[this.Count];
                entry.Sequence.Clear();
                entry.Length = 0;
                entry.Type = containerType;
                return this.array[this.Count++];
            }

            public void IncreaseCurrentContainerLength(long increase)
            {
                this.array[this.Count - 1].Length += increase;
            }

            public ContainerInfo Peek()
            {
                if (this.Count == 0)
                {
                    throw new IndexOutOfRangeException();
                }

                return this.array[this.Count - 1];
            }

            public ContainerInfo Pop()
            {
                if (this.Count == 0)
                {
                    throw new IndexOutOfRangeException();
                }

                var ret = this.array[--this.Count];
                return ret;
            }

            public void Clear()
            {
                // Don't dispose of the lists
                this.Count = 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void EnsureCapacity(int forIndex)
            {
                if (forIndex < this.array.Length)
                {
                    return;
                }

                // Resize
                Array.Resize(ref this.array, this.array.Length * 2);
            }
        }
    }
}
