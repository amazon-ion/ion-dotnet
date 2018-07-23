using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace IonDotnet.Internals
{
    internal class RawBinaryWriter : IIonWriter
    {
        private enum ContainerType
        {
            Sequence,
            Struct,
            Annotation
        }

        private const int IntZeroByte = 0x20;

        //high-bits of different value types
        private const byte PosIntTypeByte = 0x20;
        private const byte NegIntTypeByte = 0x30;
        private const byte TidListByte = 0xB0;
        private const byte TidSexpByte = 0xC0;
        private const byte TidStructByte = 0xD0;
        private const byte TidTypeDeclByte = 0xE0;
        private const byte TidStringByte = 0x80;

        private const byte NullNull = 0x0F;

        private const byte BoolFalseByte = 0x10;
        private const byte BoolTrueByte = 0x11;

        private const int DefaultContainerStackSize = 6;
        private readonly IWriteBuffer _lengthBuffer;
        private readonly IWriteBuffer _dataBuffer;
        private readonly List<SymbolToken> _annotations = new List<SymbolToken>();

        private SymbolToken _currentFieldSymbolToken;
        private readonly Stack<(List<Memory<byte>> sequence, ContainerType type, long length)> _containerStack;
        private readonly List<Memory<byte>> _lengthSegments = new List<Memory<byte>>();

        internal RawBinaryWriter(IWriteBuffer lengthBuffer, IWriteBuffer dataBuffer)
        {
            _lengthBuffer = lengthBuffer;
            _dataBuffer = dataBuffer;
            _containerStack = new Stack<(List<Memory<byte>> sequence, ContainerType type, long length)>(DefaultContainerStackSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateCurrentContainerLength(long increase)
        {
            if (_containerStack.Count == 0) return;
            //pop and push, should be quick
            var ( sequence, type, length) = _containerStack.Pop();
            _containerStack.Push((sequence, type, length + increase));
        }

        /// <summary>
        /// Prepare the field name and annotations (if any)
        /// </summary>
        /// <remarks>This method should implemented in a way that it can be called multiple times and still remains the correct state</remarks>
        private void PrepareValue()
        {
            if (IsInStruct && _currentFieldSymbolToken == default)
            {
                throw new InvalidOperationException("In a struct but field name is not set");
            }

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
                var newList = new List<Memory<byte>>();
                _containerStack.Push((newList, ContainerType.Annotation, 0));
                _dataBuffer.StartStreak(newList);

                var annotLength = _dataBuffer.WriteAnnotationsWithLength(_annotations);
                UpdateCurrentContainerLength(annotLength);

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
                if (containerInfo.type == ContainerType.Annotation)
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
            var wrappedList = _dataBuffer.Wrapup();
            Debug.Assert(ReferenceEquals(wrappedList, popped.sequence));

            if (_containerStack.Count == 0) return;

            var outer = _containerStack.Peek();

            //write the tid|len byte and (maybe) the length into the length buffer
            var idxBeforeWrite = _lengthSegments.Count - 1;
            _lengthBuffer.StartStreak(_lengthSegments);
            byte tidByte;
            switch (popped.type)
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
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var wholeContainerLength = popped.length;
            if (wholeContainerLength <= 0xD)
            {
                //fit in the tid byte
                tidByte |= (byte) wholeContainerLength;
                UpdateCurrentContainerLength(1 + wholeContainerLength);
                _lengthBuffer.WriteByte(tidByte);
            }
            else
            {
                tidByte |= IonConstants.LnIsVarLen;
                _lengthBuffer.WriteByte(tidByte);
                var lengthBytes = _lengthBuffer.WriteVarUint(popped.length);
                UpdateCurrentContainerLength(1 + lengthBytes + wholeContainerLength);
            }

            _lengthBuffer.Wrapup();
            var idxAfterWrite = _lengthSegments.Count - 1;

            for (var i = idxBeforeWrite; i <= idxAfterWrite; i++)
            {
                outer.sequence.Add(_lengthSegments[i]);
            }

            outer.sequence.AddRange(wrappedList);
            _dataBuffer.StartStreak(outer.sequence);
        }

        private void WriteVarUint(long value)
        {
            Debug.Assert(value >= 0);
            var written = _dataBuffer.WriteVarUint(value);
            UpdateCurrentContainerLength(written);
        }

        //this is not supposed to be called ever
        public ISymbolTable SymbolTable => SharedSymbolTable.GetSystem(1);

        public void Flush()
        {
            throw new NotImplementedException();
        }

        public void Finish()
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void SetFieldName(string name) => throw new NotSupportedException("Cannot set a field name here");

        public void SetFieldNameSymbol(SymbolToken name)
        {
            if (!IsInStruct) throw new IonException($"Has to be in a struct to set a field name");
            _currentFieldSymbolToken = name;
        }

        public void StepIn(IonType type)
        {
            if (!type.IsContainer()) throw new IonException($"Cannot step into {type}");

            PrepareValue();
            //wrapup the current writes

            if (_containerStack.Count > 0)
            {
                var writeList = _dataBuffer.Wrapup();
                Debug.Assert(ReferenceEquals(writeList, _containerStack.Peek().sequence));
            }

            var newList = new List<Memory<byte>>();
            _containerStack.Push((newList, type == IonType.Struct ? ContainerType.Struct : ContainerType.Sequence, 0));
        }

        public void StepOut()
        {
            if (_currentFieldSymbolToken != default) throw new IonException("Cannot step out with field name set");
            if (_annotations.Count > 0) throw new IonException("Cannot step out with annotations set");

            //TODO check if this container is actually list or struct
            PopContainer();
            //clear annotations
            FinishValue();
        }

        public bool IsInStruct => _containerStack.Count > 0 && _containerStack.Peek().type == ContainerType.Struct;

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
            PrepareValue();
            UpdateCurrentContainerLength(1);
            _dataBuffer.WriteByte(NullNull);
        }

        public void WriteNull(IonType type)
        {
            var nullByte = IonConstants.GetNullByte(type);
            PrepareValue();
            UpdateCurrentContainerLength(1);
            _dataBuffer.WriteByte(nullByte);
            FinishValue();
        }

        public void WriteBool(bool value)
        {
            PrepareValue();
            UpdateCurrentContainerLength(1);
            if (value)
            {
                _dataBuffer.WriteByte(BoolTrueByte);
            }
            else
            {
                _dataBuffer.WriteByte(BoolFalseByte);
            }
        }

        public void WriteInt(long value)
        {
            PrepareValue();
            if (value == 0)
            {
                UpdateCurrentContainerLength(1);
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
                    _dataBuffer.WriteByte(NegIntTypeByte | 0x8);
                    _dataBuffer.WriteUint64(value);
                    UpdateCurrentContainerLength(9);
                }
                else
                {
                    WriteTypedUInt(NegIntTypeByte, -value);
                }
            }
            else
            {
                WriteTypedUInt(PosIntTypeByte, value);
            }

            FinishValue();
        }

        private void WriteTypedUInt(byte type, long value)
        {
            if (value <= 0xFFL)
            {
                UpdateCurrentContainerLength(2);
                _dataBuffer.WriteUint8(type | 0x01);
                _dataBuffer.WriteUint8(value);
            }
            else if (value <= 0xFFFFL)
            {
                UpdateCurrentContainerLength(3);
                _dataBuffer.WriteUint8(type | 0x02);
                _dataBuffer.WriteUint16(value);
            }
            else if (value <= 0xFFFFFFL)
            {
                UpdateCurrentContainerLength(4);
                _dataBuffer.WriteUint8(type | 0x03);
                _dataBuffer.WriteUint24(value);
            }
            else if (value <= 0xFFFFFFFFL)
            {
                UpdateCurrentContainerLength(5);
                _dataBuffer.WriteUint8(type | 0x04);
                _dataBuffer.WriteUint32(value);
            }
            else if (value <= 0xFFFFFFFFFFL)
            {
                UpdateCurrentContainerLength(6);
                _dataBuffer.WriteUint8(type | 0x05);
                _dataBuffer.WriteUint40(value);
            }
            else if (value <= 0xFFFFFFFFFFFFL)
            {
                UpdateCurrentContainerLength(7);
                _dataBuffer.WriteUint8(type | 0x06);
                _dataBuffer.WriteUint48(value);
            }
            else if (value <= 0xFFFFFFFFFFFFFFL)
            {
                UpdateCurrentContainerLength(8);
                _dataBuffer.WriteUint8(type | 0x07);
                _dataBuffer.WriteUint56(value);
            }
            else
            {
                UpdateCurrentContainerLength(9);
                _dataBuffer.WriteUint8(type | 0x08);
                _dataBuffer.WriteUint64(value);
            }
        }

        public void WriteInt(BigInteger value)
        {
            throw new NotImplementedException();
        }

        public void WriteFloat(double value)
        {
            throw new NotImplementedException();
        }

        public void WriteDecimal(decimal value)
        {
            throw new NotImplementedException();
        }

        public void WriteTimestamp(DateTime value)
        {
            throw new NotImplementedException();
        }

        public void WriteSymbol(SymbolToken symbolToken)
        {
            throw new NotImplementedException();
        }

        public void WriteString(string value)
        {
            if (value == null)
            {
                WriteNull(IonType.String);
            }

            PrepareValue();
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
                tidByte |= IonConstants.LnIsVarLen;
                _dataBuffer.WriteByte(tidByte);
                totalSize += 1 + _dataBuffer.WriteVarUint(stringByteSize);
            }

            _dataBuffer.WriteUtf8(value.AsSpan(), stringByteSize);
            UpdateCurrentContainerLength(totalSize);

            FinishValue();
        }

        public void WriteBlob(byte[] value)
        {
            throw new NotImplementedException();
        }

        public void WriteBlob(ArraySegment<byte> value)
        {
            throw new NotImplementedException();
        }

        public void WriteClob(byte[] value)
        {
            throw new NotImplementedException();
        }

        public void WriteClob(ArraySegment<byte> value)
        {
            throw new NotImplementedException();
        }

        public void SetTypeAnnotations(params string[] annotations) => throw new NotSupportedException("raw writer does not support setting annotations as text");

        public void SetTypeAnnotationSymbols(ArraySegment<SymbolToken> annotations)
        {
            _annotations.Clear();

            foreach (var annotation in annotations)
            {
                _annotations.Add(annotation);
            }
        }

        public void AddTypeAnnotation(string annotation) => throw new NotSupportedException("raw writer does not support adding annotations");

        public void Dispose()
        {
            // this class is supposed to be used a tool for another writer wrapper, which will take care of freeing the resources
            // so nothing to do here
        }
    }
}
