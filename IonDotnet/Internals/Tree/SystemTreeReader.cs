using IonDotnet.Tree;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace IonDotnet.Internals.Tree
{
    internal class SystemTreeReader : IIonReader
    {

        protected readonly IIonValue _value;
        protected readonly ISymbolTable _systemSymbols;

        protected SystemTreeReader(IIonValue value)
        {
            _value = value;
            _systemSymbols = SharedSymbolTable.GetSystem(1);
        }

        public int CurrentDepth => throw new NotImplementedException();

        public IonType CurrentType => throw new NotImplementedException();

        public string CurrentFieldName => throw new NotImplementedException();

        public bool CurrentIsNull => throw new NotImplementedException();

        public bool IsInStruct => throw new NotImplementedException();

        public BigInteger BigIntegerValue()
        {
            return _value.BigIntegerValue;
        }

        public bool BoolValue()
        {
            return _value.BoolValue;
        }

        public BigDecimal DecimalValue()
        {
            return _value.BigDecimalValue;
        }

        public double DoubleValue()
        {
            return _value.DoubleValue;
        }

        public int GetBytes(Span<byte> buffer)
        {
            var lobSize = GetLobByteSize();
            var bufSize = buffer.Length;

            if (lobSize < 0 || bufSize < 0)
            {
                return 0;
            }
            else if (lobSize <= bufSize)
            {
                _value.Bytes().CopyTo(buffer);
                return lobSize;
            }
            else if (lobSize > bufSize)
            {
                _value.Bytes()
                    .Slice(0, bufSize - 1)
                    .CopyTo(buffer);
                return bufSize;
            }

            throw new IonException("Problem while copying the current blob value to a buffer");
        }

        public SymbolToken GetFieldNameSymbol()
        {
            return _value.FieldNameSymbol;
        }

        public IntegerSize GetIntegerSize()
        {
            return _value.IntegerSize;
        }

        public int GetLobByteSize()
        {
            return _value.ByteSize();
        }

        public virtual ISymbolTable GetSymbolTable() => _systemSymbols;

        public IEnumerable<SymbolToken> GetTypeAnnotations()
        {
            return _value.GetTypeAnnotations();
        }

        public int IntValue()
        {
            return _value.IntValue;
        }

        public long LongValue()
        {
            return _value.LongValue;
        }

        //TODO
        public IonType MoveNext()
        {
            throw new NotImplementedException();
        }

        public byte[] NewByteArray()
        {
            return _value.Bytes().ToArray();
        }

        //TODO:
        public void StepIn()
        {
            
            throw new NotImplementedException();
        }

        //TODO
        public void StepOut()
        {
            throw new NotImplementedException();
        }

        public string StringValue()
        {
            return _value.StringValue;
        }

        public SymbolToken SymbolValue()
        {
            return _value.SymbolValue;
        }

        public Timestamp TimestampValue()
        {
            return _value.TimestampValue;
        }
    }
}
