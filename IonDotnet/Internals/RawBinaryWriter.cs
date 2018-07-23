using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace IonDotnet.Internals
{
    internal class RawBinaryWriter : IIonWriter
    {
        private const int IntZeroByte = 0x20;
        private const byte PosIntTypeByte = 0x20;
        private const byte NegIntTypeByte = 0x30;

        private const int DefaultContainerStackSize = 6;
        private readonly IWriteBuffer _lengthWriteBuffer;
        private readonly IWriteBuffer _dataWriteBuffer;
        private readonly List<SymbolToken> _annotations = new List<SymbolToken>();

        private SymbolToken _currentFieldSymbolToken;
        private Stack<(IList<Memory<byte>> sequence, IonType type, long length)> _containerStack;

        internal RawBinaryWriter(IWriteBuffer lengthWriteBuffer, IWriteBuffer dataWriteBuffer)
        {
            _lengthWriteBuffer = lengthWriteBuffer;
            _dataWriteBuffer = dataWriteBuffer;
            _containerStack = new Stack<(IList<Memory<byte>> sequence, IonType type, long length)>(DefaultContainerStackSize);
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
                //TODO handle annotations

                _annotations.Clear();
            }
        }

        private void WriteVarUint(long value)
        {
            Debug.Assert(value >= 0);
            var written = _dataWriteBuffer.WriteVarUint(value);
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
            throw new NotImplementedException();
        }

        public void StepOut()
        {
            throw new NotImplementedException();
        }

        public bool IsInStruct => _containerStack.Count > 0 && _containerStack.Peek().type == IonType.Struct;

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
            throw new NotImplementedException();
        }

        public void WriteNull(IonType type)
        {
            throw new NotImplementedException();
        }

        public void WriteBool(bool value)
        {
            throw new NotImplementedException();
        }

        public void WriteInt(long value)
        {
            PrepareValue();
            if (value == 0)
            {
                UpdateCurrentContainerLength(1);
                _dataWriteBuffer.WriteByte(IntZeroByte);
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
                    _dataWriteBuffer.WriteByte(NegIntTypeByte | 0x8);
                    _dataWriteBuffer.WriteUint64(value);
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
            
            //TODO cleanup
        }

        private void WriteTypedUInt(byte type, long value)
        {
            if (value <= 0xFFL)
            {
                UpdateCurrentContainerLength(2);
                _dataWriteBuffer.WriteUint8(type | 0x01);
                _dataWriteBuffer.WriteUint8(value);
            }
            else if (value <= 0xFFFFL)
            {
                UpdateCurrentContainerLength(3);
                _dataWriteBuffer.WriteUint8(type | 0x02);
                _dataWriteBuffer.WriteUint16(value);
            }
            else if (value <= 0xFFFFFFL)
            {
                UpdateCurrentContainerLength(4);
                _dataWriteBuffer.WriteUint8(type | 0x03);
                _dataWriteBuffer.WriteUint24(value);
            }
            else if (value <= 0xFFFFFFFFL)
            {
                UpdateCurrentContainerLength(5);
                _dataWriteBuffer.WriteUint8(type | 0x04);
                _dataWriteBuffer.WriteUint32(value);
            }
            else if (value <= 0xFFFFFFFFFFL)
            {
                UpdateCurrentContainerLength(6);
                _dataWriteBuffer.WriteUint8(type | 0x05);
                _dataWriteBuffer.WriteUint40(value);
            }
            else if (value <= 0xFFFFFFFFFFFFL)
            {
                UpdateCurrentContainerLength(7);
                _dataWriteBuffer.WriteUint8(type | 0x06);
                _dataWriteBuffer.WriteUint48(value);
            }
            else if (value <= 0xFFFFFFFFFFFFFFL)
            {
                UpdateCurrentContainerLength(8);
                _dataWriteBuffer.WriteUint8(type | 0x07);
                _dataWriteBuffer.WriteUint56(value);
            }
            else
            {
                UpdateCurrentContainerLength(9);
                _dataWriteBuffer.WriteUint8(type | 0x08);
                _dataWriteBuffer.WriteUint64(value);
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
            throw new NotImplementedException();
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

        public void SetTypeAnnotations(params string[] annotations)
        {
            throw new NotImplementedException();
        }

        public void SetTypeAnnotationSymbols(ArraySegment<SymbolToken> annotations)
        {
            throw new NotImplementedException();
        }

        public void AddTypeAnnotation(string annotation)
        {
            throw new NotImplementedException();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dataWriteBuffer.Dispose();
                _lengthWriteBuffer.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
