using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading;
using static System.Diagnostics.Debug;

namespace IonDotnet.Internals
{
    internal abstract class IonRawBinaryReader : IIonReader
    {
        protected class ContainerStack
        {
            private const int DefaultContainerStackSize = 6;
            private readonly Stack<(long position, int localRemaining, int typeTid)> _stack;

            public ContainerStack()
            {
                _stack = new Stack<(long position, int localRemaining, int typeTid)>(DefaultContainerStackSize);
            }

            public void Push(int typeTid, long position, int localRemaining)
            {
                _stack.Push((position, localRemaining, typeTid));
            }

            public long GetTopPosition()
            {
                Assert(_stack.Count > 0);
                return _stack.Peek().position;
            }

            public int GetTopType()
            {
                Assert(_stack.Count > 0);
                var type = _stack.Peek().typeTid;
                if (type < 0) throw new IonException("invalid type id in parent stack");
                return type;
            }

            public int GetTopLocalRemaining()
            {
                Assert(_stack.Count > 0);
                return _stack.Peek().localRemaining;
            }

            public (long position, int localRemaining, int typeTid) Pop()
            {
                Assert(_stack.Count > 0);
                return _stack.Pop();
            }
        }

        private const int NoLimit = int.MinValue;


        protected enum State
        {
            BeforeField, // only true in structs
            BeforeTid,
            BeforeValue,
            AfterValue,
            Eof
        }

        protected State _state;
        protected readonly Stream _input;

        /// <summary>
        /// This 'might' be used to indicate the local remaining bytes of the current container
        /// </summary>
        protected int _localRemaining;

        protected bool _eof;
        protected IonType _valueType;
        protected bool _valueIsNull;
        protected bool _valueIsTrue;
        protected int _valueFieldId;
        protected int _valueTid;
        protected int _valueLength;
        protected bool _isInStruct;
        protected int _parentTid;
        protected bool _hasNextNeeded;
        protected bool _structIsOrdered;

        // top of the container stack
        protected int _containerTop;

        protected long _positionStart;
        protected long _positionLength;

        // A container stacks records 3 values: type id of container, position in the buffer, and localRemaining
        // position is stored in the first 'long' of the stack item
        // 
        protected ContainerStack _containerStack;

        protected IonRawBinaryReader(Stream input)
        {
            _input = input;

            _localRemaining = NoLimit;
            _parentTid = IonConstants.TidDatagram;
            _valueFieldId = SymbolToken.UnknownSid;
            _state = State.BeforeTid;
            _eof = false;
            _hasNextNeeded = true;
            _valueIsNull = false;
            _valueIsTrue = false;
            _isInStruct = false;
            _parentTid = 0;
            _containerTop = 0;
            _containerStack = new ContainerStack();

            _positionStart = -1;
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

        private const int BinaryVersionMarkerTid = (0xE0 & 0xff) >> 4;

        private const int BinaryVersionMarkerLen = (0xE0 & 0xff) & 0xf;

        private void ClearValue()
        {
            // TODO more values here
            _valueType = IonType.None;
            _valueTid = -1;
            _valueIsNull = false;
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
                        _valueTid = ReadTypeId();
                        if (_valueTid == IonConstants.Eof)
                        {
                            _state = State.Eof;
                            _eof = true;
                        }
                        else if (_valueTid == IonConstants.TidNopPad)
                        {
                            // skips size of pad and resets State machine
                            Skip(_valueLength);
                            ClearValue();
                        }
                        else if (_valueTid == IonConstants.TidTypedecl)
                        {
                            //bvm tid happens to be typedecl
                            if (_valueLength == BinaryVersionMarkerLen)
                            {
//                                load_version_marker();
                                _valueType = IonType.Symbol;
                            }
                            else
                            {
                                // if it's not a bvm then it's an ordinary annotated value

                                // The next call changes our positions to that of the
                                // wrapped value, but we need to remember the overall
                                // wrapper position.
                            }
                        }
                        else
                        {
                            _valueType = GetIonTypeFromCode(_valueTid);
                        }

                        break;
                    case State.BeforeValue:
                        Skip(_valueLength);
                        goto case State.AfterValue;
                    case State.AfterValue:
                        _state = _isInStruct ? State.BeforeField : State.BeforeTid;
                        break;
                    case State.Eof:
                        break;
                }
            }

            // we always get here
            _hasNextNeeded = false;
        }

        private static IonType GetIonTypeFromCode(int tid)
        {
            switch (tid)
            {
                case IonConstants.TidNull: // 0
                    return IonType.Null;
                case IonConstants.TidBoolean: // 1
                    return IonType.Bool;
                case IonConstants.TidPosInt: // 2
                case IonConstants.TidNegInt: // 3
                    return IonType.Int;
                case IonConstants.TidFloat: // 4
                    return IonType.Float;
                case IonConstants.TidDecimal: // 5
                    return IonType.Decimal;
                case IonConstants.TidTimestamp: // 6
                    return IonType.Timestamp;
                case IonConstants.TidSymbol: // 7
                    return IonType.Symbol;
                case IonConstants.TidString: // 8
                    return IonType.String;
                case IonConstants.TidClob: // 9
                    return IonType.Clob;
                case IonConstants.TidBlob: // 10 A
                    return IonType.Blob;
                case IonConstants.TidList: // 11 B
                    return IonType.List;
                case IonConstants.TidSexp: // 12 C
                    return IonType.Sexp;
                case IonConstants.TidStruct: // 13 D
                    return IonType.Struct;
                case IonConstants.TidTypedecl: // 14 E
                    return IonType.None; // we don't know yet
                default:
                    throw new IonException($"Unrecognized value type encountered: {tid}");
            }
        }

        private void Skip(int length)
        {
            if (length < 0) throw new ArgumentException(nameof(length));
            if (_localRemaining == NoLimit)
            {
                //TODO try doing better here
                for (var i = 0; i < length; i++)
                {
                    _input.ReadByte();
                }

                return;
            }

            if (length > _localRemaining)
            {
                if (_localRemaining < 1) throw new IonException("Unexpected eof");
                length = _localRemaining;
            }

            for (var i = 0; i < length; i++)
            {
                _input.ReadByte();
            }

            _localRemaining -= length;
        }

        // TODO add docs IOException
        private int ReadFieldId() => ReadVarUintOrEOF();

        /// <summary>
        /// Read the TID bytes <see href="http://amzn.github.io/ion-docs/docs/binary.html"/>
        /// </summary>
        /// <returns>Tid (type code)</returns>
        /// <exception cref="IonException">If invalid states occurs</exception>
        private int ReadTypeId()
        {
            var startOfTid = _input.Position;
            var startOfValue = startOfTid + 1;
            var tdRead = ReadByte();
            if (tdRead < 0) return IonConstants.Eof;

            var tid = IonConstants.GetTypeCode(tdRead);
            var len = IonConstants.GetLowNibble(tdRead);
            if (tid == IonConstants.TidNull && len != IonConstants.LnIsNull)
            {
                //nop pad
                if (len == IonConstants.LnIsVarLen)
                {
                    len = ReadVarUint();
                }

                _state = _isInStruct ? State.BeforeField : State.BeforeTid;
                tid = IonConstants.TidNopPad;
            }
            else if (len == IonConstants.LnIsVarLen)
            {
                len = ReadVarUint();
                startOfValue = _input.Position;
            }
            else if (tid == IonConstants.TidNull)
            {
                _valueIsNull = true;
                len = 0;
                _state = State.AfterValue;
            }
            else if (tid == IonConstants.LnIsNull)
            {
                _valueIsNull = true;
                len = 0;
                _state = State.AfterValue;
            }
            else if (tid == IonConstants.TidBoolean)
            {
                switch (len)
                {
                    default:
                        throw new IonException("Tid is bool but len is not null|true|false");
                    case IonConstants.LnBooleanTrue:
                        _valueIsTrue = true;
                        break;
                    case IonConstants.LnBooleanFalse:
                        _valueIsTrue = false;
                        break;
                }

                len = 0;
                _state = State.AfterValue;
            }
            else if (tid == IonConstants.TidStruct)
            {
                _structIsOrdered = len == 1;
                if (_structIsOrdered)
                {
                    len = ReadVarUint();
                    startOfValue = _input.Position;
                }
            }

            _valueTid = tid;
            _valueLength = len;
            _positionLength = len + (startOfValue - startOfTid);
            _positionStart = startOfTid;
            return tid;
        }

        protected int ReadVarUint()
        {
            var ret = 0;
            for (var i = 0; i < 4; i++)
            {
                var b = ReadByte();
                if (b < 0) throw new IonException($"Unexpected EOF at position {_input.Position}");

                ret = (ret << 7) | (b & 0x7F);
                if ((b & 0x80) != 0) goto Done;
            }

            //if we get here we have more bits that we have room for
            throw new OverflowException($"VarUint overflow at {_input.Position}");

            Done:
            return ret;
        }

        // TODO add docs for exceptions
        /// <summary>
        /// Try read an VarUint or returns EOF
        /// </summary>
        /// <returns>Int value</returns>
        /// <exception cref="IonException">When unexpected EOF occurs</exception>
        /// <exception cref="OverflowException">If the int does not self-limit</exception>
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
            if (_eof
                || _valueType != IonType.List
                || _valueType != IonType.Struct
                || _valueType != IonType.Sexp)
            {
                throw new InvalidOperationException($"Cannot step in value {_valueType}");
            }

            // first push place where we'll take up our next value processing when we step out
            var currentPosition = _input.Position;
            var nextPosition = currentPosition + _valueLength;
            var nextRemaining = _localRemaining;
            if (nextRemaining != NoLimit)
            {
                nextRemaining = Math.Max(0, nextRemaining - _valueLength);
            }

            _containerStack.Push(_parentTid, nextPosition, nextRemaining);
            _isInStruct = _valueTid == IonConstants.TidStruct;
            _localRemaining = _valueLength;
            _state = _isInStruct ? State.BeforeField : State.BeforeTid;
            _parentTid = _valueTid;
            ClearValue();
            _hasNextNeeded = true;
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
