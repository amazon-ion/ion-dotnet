using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using static System.Diagnostics.Debug;

namespace IonDotnet.Internals
{
    internal class IonRawBinaryReader : IIonReader
    {
        private const int NoLimit = int.MinValue;
        private const int DefaultContainerStackSize = 6;

        protected enum State
        {
            Invalid, BeforeField, // only true in structs
            BeforeTid, BeforeValue, AfterValu, Eof
        }

        protected State _state;
        protected Stream _input;

        // TODO what is this
        protected int _localRemaining;

        protected bool _eof;
        protected IonType _valueType;
        protected bool _valueNull;
        protected bool _valueTrue;
        protected int _valueFieldId;
        protected int _valueTid;
        protected int _valueLength;
        protected bool _isInStruct;
        protected int _parentTid;
        protected bool _hasNextNeeded;

        // top of the container stack
        protected int _containerTop;

        protected long _positionStart;
        protected long _positionLength;

        // A container stacks records 3 values: type id of container, position in the buffer, and localRemaining
        // position is stored in the first 'long' of the stack item
        // 
        protected Stack<(long position, int localRemaining, int typeTid)> _containerStack;

        protected IonRawBinaryReader(Stream input)
        {
            _input = input;

            _localRemaining = NoLimit;
            _parentTid = IonConstants.TidDatagram;
            _valueFieldId = SymbolToken.UnknownSid;
            _state = State.BeforeTid;
            _eof = false;
            _hasNextNeeded = true;
            _valueNull = false;
            _valueTrue = false;
            _isInStruct = false;
            _parentTid = 0;
            _containerTop = 0;
            _containerStack = new Stack<(long, int, int)>(DefaultContainerStackSize);

            _positionStart = -1;
        }

        private const long TYPE_MASK = 0xffffffff;
        private void Push(int typeTid, long position, int localRemaining)
        {
            _containerStack.Push((position, localRemaining, typeTid));
        }

        private long GetTopPosition()
        {
            Assert(_containerStack.Count > 0);
            return _containerStack.Peek().position;
        }

        private int GetTopType()
        {
            Assert(_containerStack.Count > 0);
            var type = _containerStack.Peek().typeTid;
            if (type < 0) throw new IonException("invalid type id in parent stack");
            return type;
        }

        private int GetTopLocalRemaining()
        {
            Assert(_containerStack.Count > 0);
            return _containerStack.Peek().localRemaining;
        }

        private (long position, int localRemaining, int typeTid) Pop()
        {
            Assert(_containerStack.Count > 0);
            return _containerStack.Pop();
        }

        protected bool HasNext()
        {
            if (_eof || !_hasNextNeeded) return !_eof;

            try
            {
                HasNextRaw();
                return !_eof;
            }
            catch (IOException e)
            {
                throw new IonException(e);
            }
        }

        private const int BINARY_VERSION_MARKER_TID = (0xE0 & 0xff) >> 4;

        private const int BINARY_VERSION_MARKER_LEN = (0xE0 & 0xff) & 0xf;

        private void ClearValue()
        {
            // TODO more values here
            _valueType = IonType.None;
            _valueTid = -1;
            _valueNull = false;
            _valueFieldId = SymbolToken.UnknownSid;
        }

        private void HasNextRaw()
        {
            ClearValue();
            while (_valueTid == -1 && !_eof)
            {
                switch (_state)
                {
                    default:
                        throw new IonException("should not happen");
                    case State.BeforeField:
                        Assert(_valueFieldId == SymbolToken.UnknownSid);
                        _valueFieldId = ReadFieldId();
                        if (_valueFieldId == IonConstants.Eof)
                        {
                            // FIXME why is EOF ever okay in the middle of a struct?
                            _eof = true;
                            _state = State.Eof;
                            break;
                        }
                        // fall through, continue to read tid
                        goto case State.BeforeTid;
                    case State.BeforeTid:
                        _state = State.BeforeValue;

                        break;
                }
            }

            // we always get here
            _hasNextNeeded = false;
        }

        // TODO add docs IOException
        private int ReadFieldId() => ReadVarUintOrEOF();

        // TODO add docs IOException
        private int ReadTypeId()
        {
            var startOfTid = _input.Position;
            var startOfValue = startOfTid + 1;
            var tdRead = ReadByte();
            if (tdRead < 0) return IonConstants.Eof;

            var tid = IonConstants.GetTypeCode(tdRead);
            var len = IonConstants.GetLowNibble(tdRead);
            
        }

        // TODO add docs for exceptions
        protected int ReadVarUintOrEOF()
        {
            var ret = 0;
            int b;
            if ((b = ReadByte()) < 0) return IonConstants.Eof;
            //try reading for up to 4 more bytes
            for (var i = 0; i < 4; i++)
            {
                if ((b = ReadByte()) < 0) throw new IonException($"Unexpected EOF at position {_input.Position}");
                ret = (ret << 7) | (b & 0x7F);
                if ((b & 0x80) != 0) goto Done;
            }
            //if we get here we have more bits that we have room for
            throw new OverflowException($"VarUint overflow at {_input.Position}");
        Done:
            return ret;
        }

        private int ReadByte()
        {
            if (_localRemaining != NoLimit)
            {
                if (_localRemaining < 1) return IonConstants.Eof;
                _localRemaining--;
            }
            return _input.ReadByte();
        }

        public int CurrentDepth => throw new NotImplementedException();

        public BigInteger BigIntegerValue()
        {
            throw new NotImplementedException();
        }

        public bool BoolValue()
        {
            throw new NotImplementedException();
        }

        public bool CurrentIsNull()
        {
            throw new NotImplementedException();
        }

        public DateTime DateTimeValue()
        {
            throw new NotImplementedException();
        }

        public decimal DecimalValue()
        {
            throw new NotImplementedException();
        }

        public double DoubleValue()
        {
            throw new NotImplementedException();
        }

        public int GetBytes(ArraySegment<byte> buffer)
        {
            throw new NotImplementedException();
        }

        public IonType GetCurrentType()
        {
            throw new NotImplementedException();
        }

        public string GetFieldName()
        {
            throw new NotImplementedException();
        }

        public SymbolToken GetFieldNameSymbol()
        {
            throw new NotImplementedException();
        }

        public IntegerSize GetIntegerSize()
        {
            throw new NotImplementedException();
        }

        public ISymbolTable GetSymbolTable()
        {
            throw new NotImplementedException();
        }

        public int IntValue()
        {
            throw new NotImplementedException();
        }

        public bool IsInStruct()
        {
            throw new NotImplementedException();
        }

        public int LobByteSize()
        {
            throw new NotImplementedException();
        }

        public long LongValue()
        {
            throw new NotImplementedException();
        }

        public byte[] NewByteArray()
        {
            throw new NotImplementedException();
        }

        public IonType Next()
        {
            if (_eof) return IonType.None;
            if (_hasNextNeeded)
            {
                try
                {
                    HasNextRaw();
                }
                catch (IOException e)
                {
                    throw new IonException(e);
                }
            }
            _hasNextNeeded = true;
            Assert(_valueType != IonType.None || _eof);
            return _valueType;
        }

        public void StepIn()
        {
            throw new NotImplementedException();
        }

        public void StepOut()
        {
            throw new NotImplementedException();
        }

        public string StringValue()
        {
            throw new NotImplementedException();
        }

        public SymbolToken SymbolValue()
        {
            throw new NotImplementedException();
        }
    }
}
