using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
#if !(NETSTANDARD2_0 || NET45 || NETSTANDARD1_3)
using BitConverterEx = System.BitConverter;

#endif

namespace IonDotnet.Internals.Binary
{
    internal class RawBinaryWriter : IPrivateWriter
    {
        private enum ContainerType
        {
            Sequence,
            Struct,
            Annotation,
            Datagram,

            //to be used in the case where a value is treated as a container
            Value
        }

        private const int IntZeroByte = 0x20;

        //high-bits of different value types
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


        private readonly IWriterBuffer _lengthBuffer;
        private readonly IWriterBuffer _dataBuffer;
        internal readonly List<SymbolToken> _annotations = new List<SymbolToken>();

        private SymbolToken _currentFieldSymbolToken;
        private readonly ContainerStack _containerStack;
        private readonly List<Memory<byte>> _lengthSegments;

        internal RawBinaryWriter(IWriterBuffer lengthBuffer, IWriterBuffer dataBuffer, List<Memory<byte>> lengthSegments)
        {
            _lengthBuffer = lengthBuffer;
            _dataBuffer = dataBuffer;
            _lengthSegments = lengthSegments;
            _containerStack = new ContainerStack(DefaultContainerStackSize);

            //top-level writing also requires a tracker
            var pushedContainer = _containerStack.PushContainer(ContainerType.Datagram);
            _dataBuffer.StartStreak(pushedContainer.Sequence);
        }

        /// <summary>
        /// Prepare the field name and annotations (if any)
        /// </summary>
        /// <remarks>This method should implemented in a way that it can be called multiple times and still remains the correct state</remarks>
        private void PrepareValue()
        {
            if (IsInStruct && _currentFieldSymbolToken == default)
                throw new InvalidOperationException("In a struct but field name is not set");

            if (_currentFieldSymbolToken != default)
            {
                //write field name id
                WriteVarUint(_currentFieldSymbolToken.Sid);
                _currentFieldSymbolToken = default;
            }

            if (_annotations.Count > 0)
            {
                //Since annotations 'wraps' the actual value, we basically won't know the length 
                //(the upcoming value might be another container) 
                //so we treat this as another container of type 'annotation'

                //add all written segments to the sequence
                _dataBuffer.Wrapup();

                //set a new container
                var newContainer = _containerStack.PushContainer(ContainerType.Annotation);
                _dataBuffer.StartStreak(newContainer.Sequence);

                var annotLength = _dataBuffer.WriteAnnotationsWithLength(_annotations);
                _containerStack.IncreaseCurrentContainerLength(annotLength);

                _annotations.Clear();
            }
        }

        /// <summary>
        /// This is called after the value is written, and will check if the written value is wrapped within annotations
        /// </summary>
        private void FinishValue()
        {
            if (_containerStack.Count > 0)
            {
                var containerInfo = _containerStack.Peek();
                if (containerInfo.Type == ContainerType.Annotation)
                {
                    PopContainer();
                }
            }
        }

        /// <summary>
        /// Pop a container from the container stack and link the previous container sequence with the length
        /// and sequence of the popped container
        /// </summary>
        private void PopContainer()
        {
            var popped = _containerStack.Pop();
            if (_containerStack.Count == 0)
                return;


            var wrappedList = _dataBuffer.Wrapup();
            Debug.Assert(ReferenceEquals(wrappedList, popped.Sequence));

            var outer = _containerStack.Peek();

            //write the tid|len byte and (maybe) the length into the length buffer
            //clear the length segments, no worry
            _lengthSegments.Clear();
            _lengthBuffer.StartStreak(_lengthSegments);
            byte tidByte;
            switch (popped.Type)
            {
                case ContainerType.Sequence:
                    tidByte = TidListByte;
                    break;
                case ContainerType.Struct:
                    tidByte = TidStructByte;
                    break;
                case ContainerType.Annotation:
                    tidByte = TidTypeDeclByte;
                    break;
                case ContainerType.Value:
                    tidByte = TidTimestampByte;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var wholeContainerLength = popped.Length;
            if (wholeContainerLength <= 0xD)
            {
                //fit in the tid byte
                tidByte |= (byte) wholeContainerLength;
                _containerStack.IncreaseCurrentContainerLength(1 + wholeContainerLength);
                _lengthBuffer.WriteByte(tidByte);
            }
            else
            {
                tidByte |= BinaryConstants.LnIsVarLen;
                _lengthBuffer.WriteByte(tidByte);
                var lengthBytes = _lengthBuffer.WriteVarUint(popped.Length);
                _containerStack.IncreaseCurrentContainerLength(1 + lengthBytes + wholeContainerLength);
            }

            _lengthBuffer.Wrapup();
            outer.Sequence.AddRange(_lengthSegments);

            outer.Sequence.AddRange(wrappedList);
            _dataBuffer.StartStreak(outer.Sequence);
        }

        private void WriteVarUint(long value)
        {
            Debug.Assert(value >= 0);
            var written = _dataBuffer.WriteVarUint(value);
            _containerStack.IncreaseCurrentContainerLength(written);
        }

        //this is not supposed to be called ever
        public ISymbolTable SymbolTable => SharedSymbolTable.GetSystem(1);

        /// <summary>
        /// Simply write the buffers (async), <see cref="PrepareFlush"/> should be called first
        /// </summary>
        /// <param name="outputStream">Stream to flush to</param>
        public async Task FlushAsync(Stream outputStream)
        {
            Debug.Assert(_containerStack.Count == 1, $"{_containerStack.Count}");
            Debug.Assert(outputStream?.CanWrite == true);
            var currentSequence = _containerStack.Peek().Sequence;

            //now write
            foreach (var segment in currentSequence)
            {
                await outputStream.WriteAsync(segment);
            }

            outputStream.Flush();
        }

        /// <summary>
        /// Simply write the buffers (blocking), <see cref="PrepareFlush"/> should be called first
        /// </summary>
        /// <param name="outputStream">Stream to flush to</param>
        public void Flush(Stream outputStream)
        {
            Debug.Assert(_containerStack.Count == 1, $"{_containerStack.Count}");
            Debug.Assert(outputStream?.CanWrite == true);
            var currentSequence = _containerStack.Peek().Sequence;

            //now write
            foreach (var segment in currentSequence)
            {
                outputStream.Write(segment.Span);
            }

            outputStream.Flush();
        }

        //these won't be called at this level
        Task IIonWriter.FlushAsync() => TaskEx.CompletedTask;

        void IIonWriter.Flush()
        {
        }

        /// <summary>
        /// This will stage the remaining writes in the buffer to be flushed, should be called before 'flush()'
        /// </summary>
        /// <returns>Total size of the bytes to be flushed</returns>
        internal int PrepareFlush()
        {
            var topContainer = _containerStack.Peek();
            //wrapup to append all data to the sequence
            //but first, remember the previous position so we can update the length
            var currIdx = topContainer.Sequence.Count;
            _dataBuffer.Wrapup();
            var increased = 0;
            for (var i = currIdx; i < topContainer.Sequence.Count; i++)
            {
                increased += topContainer.Sequence[i].Length;
            }

            _containerStack.IncreaseCurrentContainerLength(increased);
            return (int) topContainer.Length;
        }

        public void Finish()
        {
            //TODO implement writing again after finish
        }

        internal void Reset()
        {
            //reset the states
            _dataBuffer.Reset();
            //double calls to Reset() should be fine
            _lengthBuffer.Reset();
            _containerStack.Clear();

            //set the top-level container
            var pushedContainer = _containerStack.PushContainer(ContainerType.Datagram);
            _dataBuffer.StartStreak(pushedContainer.Sequence);
        }

        public Task FinishAsync()
        {
            throw new NotImplementedException();
        }

        public void SetFieldName(string name) => throw new NotSupportedException("Cannot set a field name here");

        public void SetFieldNameSymbol(SymbolToken symbol)
        {
            if (!IsInStruct)
                throw new IonException("Has to be in a struct to set a field name");
            _currentFieldSymbolToken = symbol;
        }

        public void StepIn(IonType type)
        {
            if (!type.IsContainer())
                throw new IonException($"Cannot step into {type}");

            PrepareValue();
            //wrapup the current writes

            if (_containerStack.Count > 0)
            {
                var writeList = _dataBuffer.Wrapup();
                Debug.Assert(ReferenceEquals(writeList, _containerStack.Peek().Sequence));
            }

            var pushedContainer = _containerStack.PushContainer(type == IonType.Struct ? ContainerType.Struct : ContainerType.Sequence);
            _dataBuffer.StartStreak(pushedContainer.Sequence);
        }

        public void StepOut()
        {
            if (_currentFieldSymbolToken != default)
                throw new IonException("Cannot step out with field name set");
            if (_annotations.Count > 0)
                throw new IonException("Cannot step out with annotations set");

            //TODO check if this container is actually list or struct
            var currentContainerType = _containerStack.Peek().Type;

            if (currentContainerType != ContainerType.Sequence && currentContainerType != ContainerType.Struct)
                throw new IonException($"Cannot step out of {currentContainerType}");

            PopContainer();
            //clear annotations
            FinishValue();
        }

        public bool IsInStruct => _containerStack.Count > 0 && _containerStack.Peek().Type == ContainerType.Struct;

        public void WriteValue(IIonReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteValues(IIonReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteNull()
        {
            const byte nullNull = 0x0F;
            PrepareValue();
            _containerStack.IncreaseCurrentContainerLength(1);
            _dataBuffer.WriteByte(nullNull);
            FinishValue();
        }

        public void WriteNull(IonType type)
        {
            var nullByte = BinaryConstants.GetNullByte(type);
            PrepareValue();
            _containerStack.IncreaseCurrentContainerLength(1);
            _dataBuffer.WriteByte(nullByte);
            FinishValue();
        }

        public void WriteBool(bool value)
        {
            PrepareValue();
            _containerStack.IncreaseCurrentContainerLength(1);
            _dataBuffer.WriteByte(value ? BoolTrueByte : BoolFalseByte);
        }

        public void WriteInt(long value)
        {
            PrepareValue();
            if (value == 0)
            {
                _containerStack.IncreaseCurrentContainerLength(1);
                _dataBuffer.WriteByte(IntZeroByte);
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
                    _dataBuffer.WriteByte(TidNegIntByte | 0x8);
                    _dataBuffer.WriteUint64(value);
                    _containerStack.IncreaseCurrentContainerLength(9);
                }
                else
                {
                    WriteTypedUInt(TidNegIntByte, -value);
                }
            }
            else
            {
                WriteTypedUInt(TidPosIntByte, value);
            }

            FinishValue();
        }

        private void WriteTypedUInt(byte type, long value)
        {
            if (value <= 0xFFL)
            {
                _containerStack.IncreaseCurrentContainerLength(2);
                _dataBuffer.WriteUint8(type | 0x01);
                _dataBuffer.WriteUint8(value);
            }
            else if (value <= 0xFFFFL)
            {
                _containerStack.IncreaseCurrentContainerLength(3);
                _dataBuffer.WriteUint8(type | 0x02);
                _dataBuffer.WriteUint16(value);
            }
            else if (value <= 0xFFFFFFL)
            {
                _containerStack.IncreaseCurrentContainerLength(4);
                _dataBuffer.WriteUint8(type | 0x03);
                _dataBuffer.WriteUint24(value);
            }
            else if (value <= 0xFFFFFFFFL)
            {
                _containerStack.IncreaseCurrentContainerLength(5);
                _dataBuffer.WriteUint8(type | 0x04);
                _dataBuffer.WriteUint32(value);
            }
            else if (value <= 0xFFFFFFFFFFL)
            {
                _containerStack.IncreaseCurrentContainerLength(6);
                _dataBuffer.WriteUint8(type | 0x05);
                _dataBuffer.WriteUint40(value);
            }
            else if (value <= 0xFFFFFFFFFFFFL)
            {
                _containerStack.IncreaseCurrentContainerLength(7);
                _dataBuffer.WriteUint8(type | 0x06);
                _dataBuffer.WriteUint48(value);
            }
            else if (value <= 0xFFFFFFFFFFFFFFL)
            {
                _containerStack.IncreaseCurrentContainerLength(8);
                _dataBuffer.WriteUint8(type | 0x07);
                _dataBuffer.WriteUint56(value);
            }
            else
            {
                _containerStack.IncreaseCurrentContainerLength(9);
                _dataBuffer.WriteUint8(type | 0x08);
                _dataBuffer.WriteUint64(value);
            }
        }

        public void WriteInt(BigInteger value)
        {
            if (value >= long.MinValue && value <= long.MaxValue)
            {
                WriteInt((long) value);
                return;
            }

            PrepareValue();

            var type = TidPosIntByte;
            if (value < 0)
            {
                type = TidNegIntByte;
                value = BigInteger.Negate(value);
            }

            //TODO is this different than java, is there a no-alloc way?
#if NET45 || NETSTANDARD1_3 || NETSTANDARD2_0
            var buffer = value.ToByteArray();
#else
            var buffer = value.ToByteArray(isUnsigned: true, isBigEndian: true);
#endif
            WriteTypedBytes(type, buffer);

            FinishValue();
        }

        /// <summary>
        /// Write raw bytes with a type.
        /// </summary>
        /// <remarks>This does not do <see cref="PrepareValue"/></remarks> or <see cref="FinishValue"/>
        private void WriteTypedBytes(byte type, ReadOnlySpan<byte> data)
        {
            var totalLength = 1;
            if (data.Length <= 0xD)
            {
                _dataBuffer.WriteUint8(type | (byte) data.Length);
            }
            else
            {
                _dataBuffer.WriteUint8(type | BinaryConstants.LnIsVarLen);
                totalLength += _dataBuffer.WriteVarUint(data.Length);
            }

            _dataBuffer.WriteBytes(data);
            _containerStack.IncreaseCurrentContainerLength(totalLength + data.Length);
        }

        public void WriteFloat(double value)
        {
            PrepareValue();

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (value == (float) value)
            {
                //TODO requires careful testing
                _containerStack.IncreaseCurrentContainerLength(5);
                _dataBuffer.WriteByte(TidFloatByte | 4);
                _dataBuffer.WriteUint32(BitConverterEx.SingleToInt32Bits((float) value));
            }
            else
            {
                _containerStack.IncreaseCurrentContainerLength(9);
                _dataBuffer.WriteByte(TidFloatByte | 8);
                _dataBuffer.WriteUint64(BitConverterEx.DoubleToInt64Bits(value));
            }

            FinishValue();
        }

        public void WriteDecimal(decimal value)
        {
            PrepareValue();

            if (value == 0)
            {
                _containerStack.IncreaseCurrentContainerLength(1);
                _dataBuffer.WriteUint8(TidDecimalByte);
            }
            else
            {
                WriteDecimalNumber(value);
            }

            FinishValue();
        }

        private void WriteDecimalNumber(decimal value)
        {
            Span<byte> bytes = stackalloc byte[sizeof(decimal)];
            var maxIdx = CopyDecimalBigEndian(bytes, value);

            var negative = value < 0;
            Debug.Assert(!negative || (bytes[0] & 0b1000_0000) <= 0);
            Debug.Assert(negative || (bytes[0] ^ 0b1000_0000) > 0);

            //len = maxid - (last index of flag=3) + (exponent byte=1)
            var totalLength = maxIdx - 2;
            var needExtraByte = (bytes[4] & 0b_1000_0000) > 0;
            if (needExtraByte)
            {
                totalLength++;
            }

            var tidByte = TidDecimalByte;
            if (totalLength <= 0x0D)
            {
                tidByte |= (byte) totalLength;
                _dataBuffer.WriteByte(tidByte);
                totalLength++;
            }
            else
            {
                tidByte |= BinaryConstants.LnIsVarLen;
                _dataBuffer.WriteByte(tidByte);
                totalLength += 1 + _dataBuffer.WriteVarUint(totalLength);
            }

            const byte isNegativeAndDone = 0b_1100_0000;
            //byte[2] is enough to store the 28 decimal places (255>28)
            _dataBuffer.WriteByte((byte) (bytes[2] | isNegativeAndDone));

            //write the 'sign' byte
            if (needExtraByte)
            {
                _dataBuffer.WriteByte((byte) (negative ? 0b_1000_0000 : 0b_0000_000));
            }
            else if (negative)
            {
                bytes[4] |= 0b_1000_0000;
            }

            _dataBuffer.WriteBytes(bytes.Slice(4, maxIdx - 3));

            _containerStack.IncreaseCurrentContainerLength(totalLength);
        }

        private static unsafe int CopyDecimalBigEndian(Span<byte> bytes, decimal value)
        {
            var p = (byte*) &value;

            //keep the flag the same
            bytes[0] = p[0];
            bytes[1] = p[1];
            bytes[2] = p[2];
            bytes[3] = p[3];

            //high
            var i = 7;
            while (i > 3 && p[i] == 0)
            {
                i--;
            }

            var hasHigh = i > 3;
            var j = 3;
            while (i > 3)
            {
                bytes[++j] = p[i--];
            }

            //mid
            i = 15;
            bool hasMid;
            if (!hasHigh)
            {
                while (i > 11 && p[i] == 0)
                {
                    i--;
                }

                hasMid = i > 11;
            }
            else
            {
                hasMid = true;
            }

            while (i > 11)
            {
                bytes[++j] = p[i--];
            }

            //lo
            i = 11;
            if (!hasMid)
            {
                while (i > 7 && p[i] == 0)
                {
                    i--;
                }
            }

            while (i > 7)
            {
                bytes[++j] = p[i--];
            }

            return j;
        }

        public void WriteTimestamp(Timestamp value)
        {
            const byte varintNegZero = 0xC0;
            const short minutePrecision = 3;
            const short secondPrecision = 4;
            const short fracPrecision = 5;

            PrepareValue();

            //wrapup first
            //add all written segments to the sequence
            _dataBuffer.Wrapup();

            //set a new container
            var newContainer = _containerStack.PushContainer(ContainerType.Value);
            var totalLength = 0;
            _dataBuffer.StartStreak(newContainer.Sequence);
            if (value.DateTimeValue.Kind == DateTimeKind.Unspecified || value.DateTimeValue.Kind == DateTimeKind.Local)
            {
                //unknown offset
                totalLength++;
                _dataBuffer.WriteByte(varintNegZero);
            }
            else
            {
                totalLength += _dataBuffer.WriteVarUint(value.LocalOffset);
            }

            _containerStack.IncreaseCurrentContainerLength(totalLength);

            //don't update totallength here
            WriteVarUint(value.DateTimeValue.Year);
            WriteVarUint(value.DateTimeValue.Month);
            WriteVarUint(value.DateTimeValue.Day);

            short precision = 0;
            //we support up to ticks precision
            decimal tickRemainder = value.DateTimeValue.Ticks % TimeSpan.TicksPerSecond;
            if (tickRemainder != 0)
            {
                precision = fracPrecision;
            }
            else if (value.DateTimeValue.Second != 0)
            {
                precision = secondPrecision;
            }
            else if (value.DateTimeValue.Hour != 0 || value.DateTimeValue.Minute != 0)
            {
                precision = minutePrecision;
            }

            if (precision >= minutePrecision)
            {
                WriteVarUint(value.DateTimeValue.Hour);
                WriteVarUint(value.DateTimeValue.Minute);
            }

            if (precision >= secondPrecision)
            {
                WriteVarUint(value.DateTimeValue.Second);
            }

            if (precision == fracPrecision)
            {
                tickRemainder /= TimeSpan.TicksPerSecond;
                WriteDecimalNumber(tickRemainder);
            }

            PopContainer();

            FinishValue();
        }

        public void WriteSymbol(string symbol)
            => throw new UnsupportedIonVersionException($"Writing text symbol is not supported at raw level");

        public void WriteSymbolToken(SymbolToken token)
        {
            if (token == default)
            {
                //does this ever happen?
                WriteNull(IonType.Symbol);
                return;
            }

            Debug.Assert(token.Sid >= 0);
            PrepareValue();
            WriteTypedUInt(TidSymbolType, token.Sid);
            FinishValue();
        }

        public void WriteString(string value)
        {
            if (value == null)
            {
                WriteNull(IonType.String);
                return;
            }

            PrepareValue();
            //TODO what's the performance implication of this?
            var stringByteSize = Encoding.UTF8.GetByteCount(value);
            //since we know the length of the string upfront, we can just write the length right here
            var tidByte = TidStringByte;
            var totalSize = stringByteSize;
            if (stringByteSize <= 0x0D)
            {
                tidByte |= (byte) stringByteSize;
                _dataBuffer.WriteByte(tidByte);
                totalSize += 1;
            }
            else
            {
                tidByte |= BinaryConstants.LnIsVarLen;
                _dataBuffer.WriteByte(tidByte);
                totalSize += 1 + _dataBuffer.WriteVarUint(stringByteSize);
            }

            _dataBuffer.WriteUtf8(value.AsSpan(), stringByteSize);
            _containerStack.IncreaseCurrentContainerLength(totalSize);

            FinishValue();
        }

        public void WriteBlob(ReadOnlySpan<byte> value)
        {
            if (value == null)
            {
                WriteNull(IonType.Blob);
                return;
            }

            PrepareValue();

            WriteTypedBytes(TidBlobByte, value);

            FinishValue();
        }

        public void WriteClob(ReadOnlySpan<byte> value)
        {
            throw new NotImplementedException();
        }

        public void SetTypeAnnotation(string annotation)
            => throw new NotSupportedException("raw writer does not support setting annotations as text");

        public void SetTypeAnnotationSymbols(IEnumerable<SymbolToken> annotations)
        {
            _annotations.Clear();

            foreach (var annotation in annotations)
            {
                _annotations.Add(annotation);
            }
        }

        internal void AddTypeAnnotationSymbol(SymbolToken annotation) => _annotations.Add(annotation);

        internal void ClearAnnotations() => _annotations.Clear();

        public void AddTypeAnnotation(string annotation) => throw new NotSupportedException("raw writer does not support adding annotations");

        public void Dispose()
        {
            _dataBuffer.Dispose();
        }

        public bool IsFieldNameSet() => _currentFieldSymbolToken != default;

        public int GetDepth() => _containerStack.Count - 1;

        public void WriteIonVersionMarker()
        {
            _dataBuffer.WriteUint32(0xE0_01_00_EA);
            _containerStack.IncreaseCurrentContainerLength(4);
        }

        public bool IsStreamCopyOptimized => throw new NotImplementedException();

        internal IWriterBuffer GetLengthBuffer() => _lengthBuffer;
        internal IWriterBuffer GetDataBuffer() => _dataBuffer;

        private class ContainerInfo
        {
            public List<Memory<byte>> Sequence;
            public ContainerType Type;
            public long Length;
        }

        private class ContainerStack
        {
            private ContainerInfo[] _array;

            public ContainerStack(int initialCapacity)
            {
                Debug.Assert(initialCapacity > 0);
                _array = new ContainerInfo[initialCapacity];
            }

            public ContainerInfo PushContainer(ContainerType containerType)
            {
                EnsureCapacity(Count);
                if (_array[Count] == null)
                {
                    _array[Count] = new ContainerInfo
                    {
                        Sequence = new List<Memory<byte>>(2),
                        Type = containerType
                    };
                    return _array[Count++];
                }

                var entry = _array[Count];
                entry.Sequence.Clear();
                entry.Length = 0;
                entry.Type = containerType;
                return _array[Count++];
            }

            public void IncreaseCurrentContainerLength(long increase)
            {
                _array[Count - 1].Length += increase;
            }

            public ContainerInfo Peek()
            {
                if (Count == 0)
                    throw new IndexOutOfRangeException();
                return _array[Count - 1];
            }

            public ContainerInfo Pop()
            {
                if (Count == 0)
                    throw new IndexOutOfRangeException();
                var ret = _array[--Count];
                return ret;
            }

            public void Clear()
            {
                //don't dispose of the lists
                Count = 0;
            }

            public int Count { get; private set; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void EnsureCapacity(int forIndex)
            {
                if (forIndex < _array.Length)
                    return;
                //resize
                Array.Resize(ref _array, _array.Length * 2);
            }
        }
    }
}
