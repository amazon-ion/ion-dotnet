using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using IonDotnet.Conversions;
using IonDotnet.Systems;

#if !(NETSTANDARD2_0 || NET45 || NETSTANDARD1_3)
using BitConverterEx = System.BitConverter;

#endif

namespace IonDotnet.Internals.Binary
{
    /// <inheritdoc />
    /// <summary>
    /// Base functionalities for Ion binary readers <br/> <see href="http://amzn.github.io/ion-docs/docs/binary.html" /> 
    /// This handles going through the stream and reading TIDs, length
    /// </summary>
    internal abstract class RawBinaryReader : IIonReader
    {
        /// <summary>
        /// The <see cref="_localRemaining"/> value for depth 0 (datagram)
        /// </summary>
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
        private readonly Stream _input;

        /// <summary>
        /// This 'might' be used to indicate the local remaining bytes of the current container
        /// </summary>
        private int _localRemaining;

        protected ValueVariant _v;

        protected bool _eof;
        protected IonType _valueType;
        protected bool _valueIsNull;
        protected bool _valueIsTrue;
        protected int _valueFieldId;
        protected int _valueTid;
        protected int _valueLength;
        protected bool _moveNextNeeded;
        private bool _structIsOrdered;
        private bool _valueLobReady;
        private int _valueLobRemaining;
        protected bool _hasSymbolTableAnnotation;

        /// <summary>
        /// A container stacks records 3 values: type id of container, position in the buffer, and localRemaining
        /// position is stored in the first 'long' of the stack item
        /// </summary>
        private readonly Stack<(int localRemaining, int typeTid)> _containerStack;

        protected readonly List<int> Annotations = new List<int>(2);

        protected RawBinaryReader(Stream input)
        {
            if (input == null || !input.CanRead)
                throw new ArgumentException("Input not readable", nameof(input));

            _input = input;

            _localRemaining = NoLimit;
            _valueFieldId = SymbolToken.UnknownSid;
            _state = State.BeforeTid;
            _eof = false;
            _moveNextNeeded = true;
            _valueIsNull = false;
            _valueIsTrue = false;
            IsInStruct = false;
            _containerStack = new Stack<(int localRemaining, int typeTid)>(DefaultContainerStackSize);

            _v = new ValueVariant();
        }

        protected virtual bool HasNext()
        {
            if (_eof || !_moveNextNeeded) return !_eof;

            try
            {
                MoveNextRaw();
                return !_eof;
            }
            catch (IOException e)
            {
                throw new IonException(e);
            }
        }

        private const int BinaryVersionMarkerTid = (0xE0 & 0xff) >> 4;

        private const int BinaryVersionMarkerLen = 0xE0 & 0xff & 0xf;

        private void ClearValue()
        {
            _valueType = IonType.None;
            _valueTid = -1;
            _valueIsNull = false;
            _v.Clear();
            _valueFieldId = SymbolToken.UnknownSid;
            _hasSymbolTableAnnotation = false;
        }

        private void MoveNextRaw()
        {
            ClearValue();
            while (_valueTid == -1 && !_eof)
            {
                switch (_state)
                {
                    default:
                        throw new IonException("should not happen");
                    case State.BeforeField:
                        Debug.Assert(_valueFieldId == SymbolToken.UnknownSid);
                        _valueFieldId = ReadFieldId();
                        if (_valueFieldId == BinaryConstants.Eof)
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
                        if (_valueTid == BinaryConstants.Eof)
                        {
                            _state = State.Eof;
                            _eof = true;
                        }
                        else if (_valueTid == BinaryConstants.TidNopPad)
                        {
                            // skips size of pad and resets State machine
                            Skip(_valueLength);
                            ClearValue();
                        }
                        else if (_valueTid == BinaryConstants.TidTypedecl)
                        {
                            //bvm tid happens to be typedecl
                            if (_valueLength == BinaryVersionMarkerLen)
                            {
                                Debug.Assert(_valueTid == BinaryVersionMarkerTid);
                                // this isn't valid for any type descriptor except the first byte
                                // of a 4 byte version marker - so lets read the rest
                                LoadVersionMarker();
                                _valueType = IonType.Symbol;
                            }
                            else
                            {
                                OnValueStart();
                                // if it's not a bvm then it's an ordinary annotated value
                                _valueType = LoadAnnotationsGotoValueType();
                            }
                        }
                        else
                        {
                            OnValueStart();
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
            _moveNextNeeded = false;
        }

        private void LoadVersionMarker()
        {
            if (ReadByte() != 0x01)
                throw new IonException("Invalid binary format");
            if (ReadByte() != 0x00)
                throw new IonException("Invalid binary format");
            if (ReadByte() != 0xea)
                throw new IonException("Invalid binary format");

            // so it's a 4 byte version marker - make it look like
            // the symbol $ion_1_0 ...
            _valueTid = BinaryConstants.TidSymbol;
            _valueLength = 0;
            _v.IntValue = SystemSymbols.Ion10Sid;
            _valueIsNull = false;
            _valueLobReady = false;
            _valueFieldId = SymbolToken.UnknownSid;
            _state = State.AfterValue;
        }

        private static IonType GetIonTypeFromCode(int tid)
        {
            switch (tid)
            {
                case BinaryConstants.TidNull: // 0
                    return IonType.Null;
                case BinaryConstants.TidBoolean: // 1
                    return IonType.Bool;
                case BinaryConstants.TidPosInt: // 2
                case BinaryConstants.TidNegInt: // 3
                    return IonType.Int;
                case BinaryConstants.TidFloat: // 4
                    return IonType.Float;
                case BinaryConstants.TidDecimal: // 5
                    return IonType.Decimal;
                case BinaryConstants.TidTimestamp: // 6
                    return IonType.Timestamp;
                case BinaryConstants.TidSymbol: // 7
                    return IonType.Symbol;
                case BinaryConstants.TidString: // 8
                    return IonType.String;
                case BinaryConstants.TidClob: // 9
                    return IonType.Clob;
                case BinaryConstants.TidBlob: // 10 A
                    return IonType.Blob;
                case BinaryConstants.TidList: // 11 B
                    return IonType.List;
                case BinaryConstants.TidSexp: // 12 C
                    return IonType.Sexp;
                case BinaryConstants.TidStruct: // 13 D
                    return IonType.Struct;
                case BinaryConstants.TidTypedecl: // 14 E
                    return IonType.None; // we don't know yet
                default:
                    throw new IonException($"Unrecognized value type encountered: {tid}");
            }
        }

        /// <summary>
        /// Skip-ahead of the input stream in the current container
        /// </summary>
        /// <param name="length">Maximum skip length</param>
        /// <exception cref="ArgumentException">When <paramref name="length"/> is less than 0</exception>
        /// <exception cref="UnexpectedEofException">If the current container has no remaining bytes</exception>
        private void Skip(int length)
        {
            if (length < 0)
                throw new ArgumentException(nameof(length));
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
                if (_localRemaining < 1)
                    throw new UnexpectedEofException();
                length = _localRemaining;
            }

            for (var i = 0; i < length; i++)
            {
                _input.ReadByte();
            }

            _localRemaining -= length;
        }

        private int ReadFieldId() => ReadVarUintOrEof(out var i) < 0 ? BinaryConstants.Eof : i;

        /// <summary>
        /// Read the TID bytes 
        /// </summary>
        /// <returns>Tid (type code)</returns>
        /// <exception cref="IonException">If invalid states occurs</exception>
        private int ReadTypeId()
        {
            var tdRead = ReadByte();
            if (tdRead < 0)
                return BinaryConstants.Eof;

            var tid = BinaryConstants.GetTypeCode(tdRead);
            if (tid == BinaryConstants.TidClob)
            {
                Console.WriteLine("trouble");
            }

            var len = BinaryConstants.GetLowNibble(tdRead);
            if (tid == BinaryConstants.TidNull && len != BinaryConstants.LnIsNull)
            {
                //nop pad
                if (len == BinaryConstants.LnIsVarLen)
                {
                    len = ReadVarUint();
                }

                _state = IsInStruct ? State.BeforeField : State.BeforeTid;
                tid = BinaryConstants.TidNopPad;
            }
            else if (len == BinaryConstants.LnIsVarLen)
            {
                len = ReadVarUint();
            }
            else if (tid == BinaryConstants.TidNull)
            {
                _valueIsNull = true;
                len = 0;
                _state = State.AfterValue;
            }
            else if (len == BinaryConstants.LnIsNull)
            {
                _valueIsNull = true;
                len = 0;
                _state = State.AfterValue;
            }
            else if (tid == BinaryConstants.TidBoolean)
            {
                switch (len)
                {
                    default:
                        throw new IonException("Tid is bool but len is not null|true|false");
                    case BinaryConstants.LnBooleanTrue:
                        _valueIsTrue = true;
                        break;
                    case BinaryConstants.LnBooleanFalse:
                        _valueIsTrue = false;
                        break;
                }

                len = 0;
                _state = State.AfterValue;
            }
            else if (tid == BinaryConstants.TidStruct)
            {
                _structIsOrdered = len == 1;
                if (_structIsOrdered)
                {
                    len = ReadVarUint();
                }
            }

            _valueTid = tid;
            _valueLength = len;
            return tid;
        }

        private int ReadVarUint()
        {
            var ret = 0;
            for (var i = 0; i < 5; i++)
            {
                var b = ReadByte();
                if (b < 0)
                    throw new UnexpectedEofException();

                ret = (ret << 7) | (b & 0x7F);
                if ((b & 0x80) != 0) goto Done;
            }

            //if we get here we have more bits that we have room for
            throw new OverflowException($"VarUint overflow at, current fieldname {CurrentFieldName}");

            Done:
            return ret;
        }

        /// <summary>
        /// Try read an VarUint or returns EOF
        /// </summary>
        /// <param name="output">Out to store the read int</param>
        /// <returns>Number of bytes read, or EOF</returns>
        /// <exception cref="IonException">When unexpected EOF occurs</exception>
        /// <exception cref="OverflowException">If the int does not self-limit</exception>
        private int ReadVarUintOrEof(out int output)
        {
            output = 0;
            int b;
            if ((b = ReadByte()) < 0)
                return BinaryConstants.Eof;
            output = (output << 7) | (b & 0x7F);
            var bn = 1;
            if ((b & 0x80) != 0)
                goto Done;
            //try reading for up to 4 more bytes
            for (var i = 0; i < 4; i++)
            {
                if ((b = ReadByte()) < 0)
                    throw new UnexpectedEofException();
                output = (output << 7) | (b & 0x7F);
                bn++;
                if ((b & 0x80) != 0)
                    goto Done;
            }

            //if we get here we have more bits that we have room for
            throw new OverflowException($"VarUint overflow, fieldname {CurrentFieldName}");

            Done:
            return bn;
        }

        /// <summary>
        /// Read a var-int
        /// </summary>
        /// <param name="val">read output is set here</param>
        /// <returns>False to mean -0 => unknown value</returns>
        private bool ReadVarInt(out int val)
        {
            val = 0;
            var isNegative = false;
            var b = ReadByte();
            if (b < 0)
                throw new UnexpectedEofException();

            if ((b & 0x40) != 0)
            {
                isNegative = true;
            }

            val = b & 0x3F;
            if ((b & 0x80) != 0)
                goto Done;

            for (var i = 0; i < 4; i++)
            {
                if ((b = ReadByte()) < 0)
                    throw new UnexpectedEofException();
                val = (val << 7) | (b & 0x7F);
                if ((b & 0x80) != 0)
                    goto Done;
            }

            //here means overflow
            throw new OverflowException();

            Done:
            //non-negative, return now
            if (!isNegative) return true;

            //negative zero -0
            if (val == 0) return false;

            //otherwise just return the negation
            val = -val;
            return true;
        }

        /// <summary>
        /// Read <paramref name="length"/> bytes, store results in a long
        /// </summary>
        /// <returns>'long' representation of the value</returns>
        /// <param name="length">number of bytes to read</param>
        /// <remarks>If the result is less than 0, 64bit is not enough</remarks>
        protected long ReadUlong(int length)
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

        protected BigDecimal ReadBigDecimal(int length)
        {
            if (length == 0)
                return new BigDecimal(0m);

            var saveLimit = _localRemaining - length;
            _localRemaining = length;

            ReadVarInt(out var exponent);
            //we care about the scale here
            exponent = -exponent;

            BigInteger mag;
            var negative = false;
            if (_localRemaining == 0)
            {
                mag = BigInteger.Zero;
            }
            else
            {
                var bytes = new byte[_localRemaining];
                ReadAll(bytes, _localRemaining);

                if ((bytes[0] & 0b_1000_0000) > 0)
                {
                    //value is negative
                    bytes[0] &= 0x7F;
                    negative = true;
                }

                Array.Reverse(bytes);
                mag = new BigInteger(bytes);
                if (negative)
                {
                    mag = BigInteger.Negate(mag);
                }
            }

            _localRemaining = saveLimit;
            if (negative && mag == 0)
            {
                return BigDecimal.NegativeZero(exponent);
            }

            return new BigDecimal(mag, exponent);
        }

        protected decimal ReadDecimal(int length)
        {
            if (length == 0)
                return 0m;

            var saveLimit = _localRemaining - length;
            _localRemaining = length;

            ReadVarInt(out var exponent);
            if (exponent > 0)
                throw new IonException($"Exponent should be <= 0: {exponent}");
            //we care about the scale here
            exponent = -exponent;

            decimal dec;

            //don't support big decimal this for now
            if (exponent > 28)
                throw new OverflowException($"Decimal exponent scale {exponent} is not supported");
            if (_localRemaining > sizeof(int) * 3)
                throw new OverflowException($"Decimal mantissa size {_localRemaining} is not supported");

            if (_localRemaining == 0)
            {
                dec = new decimal(0, 0, 0, false, (byte) exponent);
            }
            else
            {
                var mantissaSize = _localRemaining;
                Span<byte> mantissaBytes = stackalloc byte[sizeof(int) * 3];
                ReadAll(mantissaBytes, _localRemaining);


                var isNegative = (mantissaBytes[0] & 0b1000_0000) > 0;
                mantissaBytes[0] &= 0x7F;
                //                Array.Reverse(mantissaBytes);

                int high = 0, mid = 0;

                var rl = mantissaSize > sizeof(int) ? sizeof(int) : mantissaSize;
                var offset = mantissaSize - rl;
                var low = ReadBigEndian(mantissaBytes.Slice(offset, rl));
                if (offset > 0)
                {
                    rl = offset > sizeof(int) ? sizeof(int) : offset;
                    offset -= rl;
                    mid = ReadBigEndian(mantissaBytes.Slice(offset, rl));
                }

                if (offset > 0)
                {
                    Debug.Assert(offset <= sizeof(int));
                    high = ReadBigEndian(mantissaBytes.Slice(0, offset));
                }

                dec = new decimal(low, mid, high, isNegative, (byte) exponent);
            }

            _localRemaining = saveLimit;
            return dec;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ReadBigEndian(Span<byte> bytes)
        {
            var ret = 0;
            foreach (var t in bytes)
            {
                ret <<= 8;
                ret |= t;
            }

            return ret;
        }

        protected Timestamp ReadTimeStamp(int length)
        {
            Debug.Assert(length > 0);

            int month = 0, day = 0, hour = 0, minute = 0, second = 0;
            decimal frac = 0;

            var saveLimit = _localRemaining - length;
            _localRemaining = length; // > 0

            var offsetKnown = ReadVarInt(out var offset);
            var year = ReadVarUint();
            if (_localRemaining > 0)
            {
                month = ReadVarUint();
                if (_localRemaining > 0)
                {
                    day = ReadVarUint();

                    // now we look for hours and minutes
                    if (_localRemaining > 0)
                    {
                        hour = ReadVarUint();
                        minute = ReadVarUint();
                        if (_localRemaining > 0)
                        {
                            second = ReadVarUint();
                            if (_localRemaining > 0)
                            {
                                // now we read in our actual "milliseconds since the epoch"
                                frac = ReadDecimal(_localRemaining);
                            }
                        }
                    }
                }
            }

            _localRemaining = saveLimit;
            if (frac > 0)
            {
                return offsetKnown
                    ? new Timestamp(year, month, day, hour, minute, second, offset, frac)
                    : new Timestamp(year, month, day, hour, minute, second, frac);
            }

            return offsetKnown
                ? new Timestamp(year, month, day, hour, minute, second, offset)
                : new Timestamp(year, month, day, hour, minute, second);
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
            if (length == 0)
                return BigInteger.Zero;

            var bytes = new byte[length];
            ReadAll(bytes, length);
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

            if (length != 4 && length != 8)
                throw new IonException($"Float length must be 0|4|8, length is {length}");
            var bits = ReadUlong(length);
            return length == 4 ? BitConverterEx.Int32BitsToSingle((int) bits) : BitConverterEx.Int64BitsToDouble(bits);
        }

        /// <summary>
        /// Load the annotations of the current value into
        /// </summary>
        /// <returns>Number of annotations</returns>
        private void LoadAnnotations(int annotLength)
        {
            // the java impl allows skipping the annotations so we can read it even if
            // _state == AfterValue. We don't allow that here
            if (_state != State.BeforeValue)
                throw new InvalidOperationException("Value is not ready");

            //reset the annotation list
            Annotations.Clear();

            int l;
            while (annotLength > 0 && (l = ReadVarUintOrEof(out var a)) != BinaryConstants.Eof)
            {
                annotLength -= l;
                if (a == SystemSymbols.IonSymbolTableSid)
                {
                    _hasSymbolTableAnnotation = true;
                }

                Annotations.Add(a);
            }
        }

        /// <summary>
        /// This method will read the annotations, and load them if requested.
        /// <para/> Then it will skip to the value
        /// </summary>
        /// <returns>Type of the value</returns>
        private IonType LoadAnnotationsGotoValueType()
        {
            //Values can be wrapped by annotations http://amzn.github.io/ion-docs/docs/binary.html#annotations
            //This is invoked when we get a typedecl tid, which means there are potentially annotations
            //Depending on the options we might load them or not, the default should be not to load them
            //In which case we'll just go through to the wrapped value
            //There is no save point here, so this either loads the annotations, or it doesnt
            var annotLength = ReadVarUint();
            LoadAnnotations(annotLength);

            // this will both get the type id and it will reset the
            // length as well (over-writing the len + annotations value
            // that is there now, before the call)
            _valueTid = ReadTypeId();
            if (_valueTid == BinaryConstants.TidNopPad)
                throw new IonException("NOP padding is not allowed within annotation wrappers");
            if (_valueTid == BinaryConstants.Eof)
                throw new UnexpectedEofException();
            if (_valueTid == BinaryConstants.TidTypedecl)
                throw new IonException("An annotation wrapper may not contain another annotation wrapper.");

            var valueType = GetIonTypeFromCode(_valueTid);
            return valueType;
        }

        // Probably the fastest way
        //        private static unsafe float Int32BitsToSingle(int value) => *(float*) (&value);

        /// <summary>
        /// Read the string value at the current position (and advance the stream by <paramref name="length"/>
        /// </summary>
        /// <param name="length">Length of the string representation in bytes</param>
        /// <returns>Read string</returns>
        protected string ReadString(int length) => length <= BinaryConstants.ShortStringLength
            ? ReadShortString(length)
            : ReadLongString(length);

        private string ReadShortString(int length)
        {
            Span<byte> alloc = stackalloc byte[BinaryConstants.ShortStringLength];
            ReadAll(alloc, length);
            ReadOnlySpan<byte> readOnlySpan = alloc;
            var strValue = Encoding.UTF8.GetString(readOnlySpan.Slice(0, length));
            return strValue;
        }

        private string ReadLongString(int length)
        {
            var alloc = ArrayPool<byte>.Shared.Rent(length);
            ReadAll(new ArraySegment<byte>(alloc, 0, length), length);

            var strValue = Encoding.UTF8.GetString(alloc, 0, length);
            ArrayPool<byte>.Shared.Return(alloc);
            return strValue;
        }

        /// <summary>
        /// Read all <paramref name="length"/> bytes into the buffer
        /// </summary>
        /// <param name="bufferSpan">Span buffer</param>
        /// <param name="length">Amount of bytes to read</param>
        private void ReadAll(Span<byte> bufferSpan, int length)
        {
            Debug.Assert(length <= bufferSpan.Length);
            while (length > 0)
            {
                var amount = _input.Read(bufferSpan.Slice(0, length));
                length -= amount;
                bufferSpan = bufferSpan.Slice(amount);
                _localRemaining -= amount;
            }
        }

        private int ReadByte()
        {
            if (_localRemaining != NoLimit)
            {
                if (_localRemaining < 1) return BinaryConstants.Eof;
                _localRemaining--;
            }

            return _input.ReadByte();
        }

        private int ReadBytesIntoBuffer(Span<byte> buffer, int length)
        {
            if (_localRemaining == NoLimit)
                return _input.Read(buffer);

            if (length > _localRemaining)
            {
                if (_localRemaining < 1)
                    throw new UnexpectedEofException();
            }

            var bytesRead = _input.Read(buffer);
            _localRemaining -= bytesRead;
            return bytesRead;
        }

        public int CurrentDepth => _containerStack.Count;

        public bool CurrentIsNull => _valueIsNull;

        public int GetBytes(Span<byte> buffer)
        {
            var length = GetLobByteSize();
            if (length > buffer.Length)
            {
                length = buffer.Length;
            }

            if (_valueLobRemaining < 1)
                return 0;

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

        public IEnumerable<SymbolToken> GetTypeAnnotations()
        {
            foreach (var aid in Annotations)
            {
                var text = GetSymbolTable().FindKnownSymbol(aid);

                yield return new SymbolToken(text, aid);
            }
        }

        public abstract bool TryConvertTo(Type targeType, IScalarConverter scalarConverter, out object result);

        public IonType CurrentType => _valueType;

        public bool IsInStruct { get; private set; }

        public int GetLobByteSize()
        {
            if (_valueType != IonType.Blob && _valueType != IonType.Clob)
                throw new InvalidOperationException($"No byte size for type {_valueType}");

            if (!_valueLobReady)
            {
                _valueLobRemaining = _valueIsNull ? 0 : _valueLength;
                _valueLobReady = true;
            }

            return _valueLobRemaining;
        }

        public byte[] NewByteArray()
        {
            var length = GetLobByteSize();
            if (_valueIsNull) return null;
            var bytes = new byte[length];
            GetBytes(bytes);
            return bytes;
        }

        public virtual IonType MoveNext()
        {
            if (_eof)
                return IonType.None;
            if (_moveNextNeeded)
            {
                try
                {
                    MoveNextRaw();
                }
                catch (IOException e)
                {
                    throw new IonException(e);
                }
            }

            _moveNextNeeded = true;
            Debug.Assert(_valueType != IonType.None || _eof);
            return _valueType;
        }

        public void StepIn()
        {
            if (_eof)
                throw new InvalidOperationException("Reached the end of the stream");
            if (!_valueType.IsContainer())
                throw new InvalidOperationException($"{_valueType} is no container");

            // first push place where we'll take up our next value processing when we step out
            var nextRemaining = _localRemaining;
            if (nextRemaining != NoLimit)
            {
                nextRemaining = Math.Max(0, nextRemaining - _valueLength);
            }

            _containerStack.Push((nextRemaining, _valueTid));
            IsInStruct = _valueTid == BinaryConstants.TidStruct;
            _localRemaining = _valueLength;
            _state = IsInStruct ? State.BeforeField : State.BeforeTid;
//            _parentTid = _valueTid;
            ClearValue();
            _moveNextNeeded = true;
        }

        public void StepOut()
        {
            if (CurrentDepth < 1)
                throw new InvalidOperationException("Cannot step out, current depth is 0");

            var (parentRemaining, _) = _containerStack.Pop();
            var parentTid = _containerStack.Count == 0 ? BinaryConstants.TidDatagram : _containerStack.Peek().typeTid;

            _eof = false;
//            _parentTid = parentTid;
            IsInStruct = parentTid == BinaryConstants.TidStruct;
            _state = IsInStruct ? State.BeforeField : State.BeforeTid;
            _moveNextNeeded = true;
            ClearValue();

            if (_localRemaining > 0)
            {
                //didn't read all the previous container
                //skip all the remaining bytes
                var distance = _localRemaining;
                Debug.Assert(distance == _localRemaining);
                const int maxSkip = int.MaxValue - 1;
                while (distance > maxSkip)
                {
                    Skip(maxSkip);
                    distance -= maxSkip;
                }

                if (distance > 0)
                {
                    Debug.Assert(distance < int.MaxValue);
                    Skip(distance);
                }
            }

            _localRemaining = parentRemaining;
        }

        public abstract string StringValue();
        public abstract long LongValue();
        public abstract SymbolToken SymbolValue();
        public abstract string CurrentFieldName { get; }
        public abstract SymbolToken GetFieldNameSymbol();
        public abstract IntegerSize GetIntegerSize();
        public abstract ISymbolTable GetSymbolTable();
        public abstract int IntValue();
        public abstract BigInteger BigIntegerValue();
        public abstract bool BoolValue();
        public abstract Timestamp TimestampValue();
        public abstract BigDecimal DecimalValue();
        public abstract double DoubleValue();

        protected abstract void OnValueStart();
        protected abstract void OnAnnotation(int annotationId);
        protected abstract void OnValueEnd();
    }
}
