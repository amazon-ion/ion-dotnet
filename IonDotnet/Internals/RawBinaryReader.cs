using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using IonDotnet.Systems;
using static System.Diagnostics.Debug;

namespace IonDotnet.Internals
{
    /// <summary>
    /// Base functionalities for Ion binary readers <see href="http://amzn.github.io/ion-docs/docs/binary.html"/>
    /// This handles going through the stream and reading TIDs, length
    /// </summary>
    internal abstract class RawBinaryReader : IIonReader
    {
        private const int NoLimit = int.MinValue;
        private const int DefaultContainerStackSize = 6;

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

        protected ValueVariant _v;

        protected bool _eof;
        protected IonType _valueType;
        protected bool _valueIsNull;
        protected bool _valueIsTrue;
        protected int _valueFieldId;
        protected int _valueTid;
        protected int _valueLength;
        protected int _parentTid;
        protected bool _hasNextNeeded;
        protected bool _structIsOrdered;

        protected bool _valueLobReady;
        protected int _valueLobRemaining;

        // top of the container stack
        protected int _containerTop;

        protected long _positionStart;
        protected long _positionLength;

        protected int[] _annotationIds;

        // A container stacks records 3 values: type id of container, position in the buffer, and localRemaining
        // position is stored in the first 'long' of the stack item
        // 
        private readonly Stack<(long position, int localRemaining, int typeTid)> _containerStack;

        protected RawBinaryReader(Stream input)
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
            IsInStruct = false;
            _parentTid = 0;
            _containerTop = 0;
            _containerStack = new Stack<(long position, int localRemaining, int typeTid)>();

            _positionStart = -1;
        }

        // TODO this doesnt make a lot of sense, should be MoveNext()
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
                        _state = IsInStruct ? State.BeforeField : State.BeforeTid;
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
                if (_localRemaining < 1) throw new UnexpectedEofException(_input.Position);
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
        /// Read the TID bytes 
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

                _state = IsInStruct ? State.BeforeField : State.BeforeTid;
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
                if (b < 0) throw new UnexpectedEofException(_input.Position);

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
                if ((b = ReadByte()) < 0) throw new UnexpectedEofException(_input.Position);
                ret = (ret << 7) | (b & 0x7F);
                if ((b & 0x80) != 0) goto Done;
            }

            //if we get here we have more bits that we have room for
            throw new OverflowException($"VarUint overflow at {_input.Position}");

            Done:
            return ret;
        }

        /// <summary>
        /// Read <paramref name="length"/> bytes, store results in a long
        /// </summary>
        /// <returns>'long' representation of the value</returns>
        /// <param name="length">number of bytes to read</param>
        /// <remarks>If the result is less than 0, 64bit is not enough</remarks>
        protected long ReadLong(int length)
        {
            long ret = 0;
            int b;
            switch (length)
            {
                default:
                    throw new ArgumentOutOfRangeException(nameof(length), "length must be <=8");
                case 8:
                    if ((b = ReadByte()) < 0) throw new UnexpectedEofException();
                    ret = (ret << 8) | (uint) b;
                    goto case 7;
                case 7:
                    if ((b = ReadByte()) < 0) throw new UnexpectedEofException();
                    ret = (ret << 8) | (uint) b;
                    goto case 6;
                case 6:
                    if ((b = ReadByte()) < 0) throw new UnexpectedEofException();
                    ret = (ret << 8) | (uint) b;
                    goto case 5;
                case 5:
                    if ((b = ReadByte()) < 0) throw new UnexpectedEofException();
                    ret = (ret << 8) | (uint) b;
                    goto case 4;
                case 4:
                    if ((b = ReadByte()) < 0) throw new UnexpectedEofException();
                    ret = (ret << 8) | (uint) b;
                    goto case 3;
                case 3:
                    if ((b = ReadByte()) < 0) throw new UnexpectedEofException();
                    ret = (ret << 8) | (uint) b;
                    goto case 2;
                case 2:
                    if ((b = ReadByte()) < 0) throw new UnexpectedEofException();
                    ret = (ret << 8) | (uint) b;
                    goto case 1;
                case 1:
                    if ((b = ReadByte()) < 0) throw new UnexpectedEofException();
                    ret = (ret << 8) | (uint) b;
                    goto case 0;
                case 0:
                    break;
            }

            return ret;
        }

        /// <summary>
        /// Read <paramref name="length"/> bytes into a <see cref="BigInteger"/>
        /// </summary>
        /// <returns>The big integer.</returns>
        /// <param name="length">Number of bytes</param>
        /// <param name="isNegative">Sign of the value</param>
        protected BigInteger ReadBigInteger(int length, bool isNegative)
        {
            //TODO this is bad, do better
            if (length == 0) return BigInteger.Zero;

            var bytes = new byte[length];
            ReadAll(new ArraySegment<byte>(bytes, 0, length), length);
            Array.Reverse(bytes);
            var bigInt = new BigInteger(bytes);
            return isNegative ? BigInteger.Negate(bigInt) : bigInt;
        }

        /// <summary>
        /// Read <paramref name="length"/> bytes into a float
        /// </summary>
        /// <returns>The float.</returns>
        /// <param name="length">Length.</param>
        protected double ReadFloat(int length)
        {
            if (length == 0) return 0;

            if (length != 4 && length != 8) throw new IonException($"Float length must be 0|4|8, length is {length}");
            var bits = ReadLong(length);
            return length == 4 ? Int32BitsToSingle((int) bits) : BitConverter.Int64BitsToDouble(bits);
        }

        /// <summary>
        /// Load the annotations of the current value into
        /// </summary>
        /// <returns>Number of annotations</returns>
        protected int LoadAnnotations()
        {
            // the java impl allows skipping the annotations so we can read it even if
            // _state == AfterValue. We don't allow that here
            if (_state != State.BeforeValue) throw new InvalidOperationException("Value is not ready");

            throw new NotImplementedException();
        }

        // Probably the fastest way
        private static unsafe float Int32BitsToSingle(int value) => *(float*) (&value);

        /// <summary>
        /// Read the string value at the current position (and advance the stream by <paramref name="length"/>)
        /// </summary>
        /// <param name="length">Length of the string representation in bytes</param>
        /// <returns>Read string</returns>
        protected String ReadString(int length)
        {
            Assert(_state == State.BeforeValue);
            //TODO consider using pipelines to avoid rebuffering
            var alloc = ArrayPool<byte>.Shared.Rent(length);
            ReadAll(new ArraySegment<byte>(alloc, 0, length), length);

            var strValue = Encoding.UTF8.GetString(alloc, 0, length);
            ArrayPool<byte>.Shared.Return(alloc);
            return strValue;
        }

        /// <summary>
        /// Reads <paramref name="length"/> bytes to the buffer, blocking till done
        /// </summary>
        /// <param name="buffer">Buffer to read data in</param>
        /// <param name="length">Number of bytes to read</param>
        /// <exception cref="UnexpectedEofException">If EOF occurs before all bytes are read</exception>
        private void ReadAll(ArraySegment<byte> buffer, int length)
        {
            Assert(length >= buffer.Count);
            var offset = buffer.Offset;
            while (length > 0)
            {
                var amount = _input.Read(buffer.Array, buffer.Offset, length);
                if (amount <= 0) throw new UnexpectedEofException(_input.Position);

                length -= amount;
                offset += amount;
            }
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

        private int ReadBytesIntoBuffer(ArraySegment<byte> buffer, int length)
        {
            if (_localRemaining == NoLimit) return _input.Read(buffer.Array, buffer.Offset, length);

            if (length > _localRemaining)
            {
                if (_localRemaining < 1) throw new UnexpectedEofException(_input.Position);
                length = _localRemaining;
            }

            var bytesRead = _input.Read(buffer.Array, buffer.Offset, length);
            _localRemaining -= bytesRead;
            return bytesRead;
        }

        public int CurrentDepth => _containerStack.Count;

        public bool CurrentIsNull => _valueIsNull;

        public int GetBytes(ArraySegment<byte> buffer)
        {
            var length = GetLobByteSize();
            if (length > buffer.Count)
            {
                length = buffer.Count;
            }

            if (_valueLobRemaining < 1) return 0;

            var readBytes = ReadBytesIntoBuffer(buffer, length);
            _valueLobRemaining -= readBytes;
            if (_valueLobRemaining == 0)
            {
                _state = State.AfterValue;
            }
            else
            {
                _valueLength = _valueLobRemaining;
            }

            return readBytes;
        }

        public IonType GetCurrentType() => _valueType;


        public bool IsInStruct { get; protected set; }

        public int GetLobByteSize()
        {
            //TODO should we do sth abt this code?
            if (_valueType != IonType.Blob && _valueType != IonType.Clob)
                throw new InvalidOperationException($"No byte size for type {_valueType}");

            if (!_valueLobReady)
            {
                _valueLobRemaining = _valueIsNull ? 0 : _valueLength;
                _valueLobReady = true;
            }

            return _valueLobRemaining;
        }

        public abstract long LongValue();

        public byte[] NewByteArray()
        {
            var length = GetLobByteSize();
            if (_valueIsNull) return null;
            var bytes = new byte[length];
            GetBytes(new ArraySegment<byte>(bytes, 0, length));
            return bytes;
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
            if (_eof) throw new InvalidOperationException("Reached the end of the stream");
            if (_valueType != IonType.List
                && _valueType != IonType.Struct
                && _valueType != IonType.Sexp)
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

            _containerStack.Push((nextPosition, nextRemaining, _parentTid));
            IsInStruct = _valueTid == IonConstants.TidStruct;
            _localRemaining = _valueLength;
            _state = IsInStruct ? State.BeforeField : State.BeforeTid;
            _parentTid = _valueTid;
            ClearValue();
            _hasNextNeeded = true;
        }

        public void StepOut()
        {
            if (CurrentDepth < 1) throw new InvalidOperationException("Cannot step out, current depth is 0");

            var (nextPosition, localRemaining, parentTid) = _containerStack.Pop();
            _eof = false;
            _parentTid = parentTid;
            IsInStruct = _parentTid == IonConstants.TidStruct;
            _state = IsInStruct ? State.BeforeField : State.BeforeTid;
            _hasNextNeeded = true;
            ClearValue();

            var currentPosition = _input.Position;
            if (nextPosition > currentPosition)
            {
                //didn't read all the previous container
                //skip all the remaining bytes
                var distance = nextPosition - currentPosition;
                var maxSkip = int.MaxValue - 1;
                while (distance > maxSkip)
                {
                    Skip(maxSkip);
                    distance -= maxSkip;
                }

                if (distance > 0)
                {
                    Assert(distance < int.MaxValue);
                    Skip((int) distance);
                }
            }
            else if (nextPosition < currentPosition)
            {
                throw new IonException($"Invalid position during stepout, curr:{currentPosition}, next:{nextPosition}");
            }

            _localRemaining = localRemaining;
        }

        public abstract string StringValue();

        public abstract SymbolToken SymbolValue();

        public abstract string GetFieldName();

        public abstract SymbolToken GetFieldNameSymbol();

        public abstract IntegerSize GetIntegerSize();

        public abstract ISymbolTable GetSymbolTable();

        public abstract int IntValue();

        public abstract BigInteger BigIntegerValue();

        public abstract bool BoolValue();

        public abstract DateTime DateTimeValue();

        public abstract decimal DecimalValue();

        public abstract double DoubleValue();
    }
}