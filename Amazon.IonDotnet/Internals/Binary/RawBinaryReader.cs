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
    using System.Buffers;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Text;
    using Amazon.IonDotnet.Internals.Conversions;
    using Amazon.IonDotnet.Utils;

    /// <inheritdoc />
    /// <summary>
    /// Base functionalities for Ion binary readers <br/> <see href="http://amzn.github.io/ion-docs/docs/binary.html" />
    /// This handles going through the stream and reading TIDs, length.
    /// </summary>
    internal abstract class RawBinaryReader : IIonReader
    {
        /// <summary>
        /// The <see cref="localRemaining"/> value for depth 0 (datagram).
        /// </summary>
        protected readonly List<int> Annotations = new List<int>(2);

        protected State state;

        protected ValueVariant valueVariant;

        protected bool eof;
        protected IonType valueType;
        protected bool valueIsNull;
        protected bool valueIsTrue;
        protected int valueFieldId;
        protected int valueTid;
        protected int valueLength;
        protected bool moveNextNeeded;
        protected bool hasSymbolTableAnnotation;

        private const int NoLimit = int.MinValue;

        private const int DefaultContainerStackSize = 6;

        private const int BinaryVersionMarkerTid = (0xE0 & 0xff) >> 4;
        private const int BinaryVersionMarkerLen = 0xE0 & 0xff & 0xf;

        private readonly Stream input;

        /// <summary>
        /// A container stacks records 3 values: type id of container, position in the buffer, and localRemaining
        /// position is stored in the first 'long' of the stack item.
        /// </summary>
        private readonly Stack<(int localRemaining, int typeTid)> containerStack;

        /// <summary>
        /// This 'might' be used to indicate the local remaining bytes of the current container.
        /// </summary>
        private int localRemaining;

        private bool structIsOrdered;
        private bool valueLobReady;
        private int valueLobRemaining;

        protected RawBinaryReader(Stream input)
        {
            if (input == null || !input.CanRead)
            {
                throw new ArgumentException("Input not readable", nameof(input));
            }

            this.input = input;

            this.localRemaining = NoLimit;
            this.valueFieldId = SymbolToken.UnknownSid;
            this.state = State.BeforeTid;
            this.eof = false;
            this.moveNextNeeded = true;
            this.valueType = IonType.None;
            this.valueIsNull = false;
            this.valueIsTrue = false;
            this.IsInStruct = false;
            this.containerStack = new Stack<(int localRemaining, int typeTid)>(DefaultContainerStackSize);

            this.valueVariant = default;
        }

        protected enum State
        {
            BeforeField, // only true in structs
            BeforeTid,
            BeforeValue,
            AfterValue,
            Eof,
        }

        public int CurrentDepth => this.containerStack.Count;

        public bool CurrentIsNull => this.valueIsNull;

        public IonType CurrentType => this.valueType;

        public abstract string CurrentFieldName { get; }

        public bool IsInStruct { get; private set; }

        /// <summary>
        /// Dispose RawBinaryReader.
        /// </summary>
        public void Dispose()
        {
            return;
        }

        public int GetBytes(Span<byte> buffer)
        {
            var length = this.GetLobByteSize();
            if (length > buffer.Length)
            {
                length = buffer.Length;
            }

            if (this.valueLobRemaining < 1)
            {
                return 0;
            }

            var readBytes = this.ReadBytesIntoBuffer(buffer, length);
            this.valueLobRemaining -= readBytes;
            if (this.valueLobRemaining == 0)
            {
                this.state = State.AfterValue;
            }
            else
            {
                this.valueLength = this.valueLobRemaining;
            }

            return readBytes;
        }

        public int GetLobByteSize()
        {
            if (this.valueType != IonType.Blob && this.valueType != IonType.Clob)
            {
                throw new InvalidOperationException($"No byte size for type {this.valueType}");
            }

            if (!this.valueLobReady)
            {
                this.valueLobRemaining = this.valueIsNull ? 0 : this.valueLength;
                this.valueLobReady = true;
            }

            return this.valueLobRemaining;
        }

        public byte[] NewByteArray()
        {
            var length = this.GetLobByteSize();
            if (this.valueIsNull)
            {
                return null;
            }

            var bytes = new byte[length];
            this.GetBytes(bytes);
            return bytes;
        }

        public virtual IonType MoveNext()
        {
            if (this.eof)
            {
                return IonType.None;
            }

            if (this.moveNextNeeded)
            {
                try
                {
                    this.MoveNextRaw();
                }
                catch (IOException e)
                {
                    throw new IonException(e);
                }
            }

            this.moveNextNeeded = true;
            Debug.Assert(this.valueType != IonType.None || this.eof, "This has reached EOF or the valueType is None");
            return this.valueType;
        }

        public void StepIn()
        {
            if (this.eof)
            {
                throw new InvalidOperationException("Reached the end of the stream");
            }

            if (!this.valueType.IsContainer())
            {
                throw new InvalidOperationException($"{this.valueType} is no container");
            }

            // First push place where we'll take up our next value processing when we step out
            var nextRemaining = this.localRemaining;
            if (nextRemaining != NoLimit)
            {
                nextRemaining = Math.Max(0, nextRemaining - this.valueLength);
            }

            this.containerStack.Push((nextRemaining, this.valueTid));
            this.IsInStruct = this.valueTid == BinaryConstants.TidStruct;
            this.localRemaining = this.valueLength;
            this.state = this.IsInStruct ? State.BeforeField : State.BeforeTid;
            this.ClearValue();
            this.moveNextNeeded = true;
        }

        public void StepOut()
        {
            if (this.CurrentDepth < 1)
            {
                throw new InvalidOperationException("Cannot step out, current depth is 0");
            }

            var (parentRemaining, _) = this.containerStack.Pop();
            var parentTid = this.containerStack.Count == 0 ? BinaryConstants.TidDatagram : this.containerStack.Peek().typeTid;

            this.eof = false;
            this.IsInStruct = parentTid == BinaryConstants.TidStruct;
            this.state = this.IsInStruct ? State.BeforeField : State.BeforeTid;
            this.moveNextNeeded = true;
            this.ClearValue();

            if (this.localRemaining > 0)
            {
                // Didn't read all of the previous container
                // skip all the remaining bytes
                var distance = this.localRemaining;
                Debug.Assert(distance == this.localRemaining, "distance does not match localRemaining");
                const int maxSkip = int.MaxValue - 1;
                while (distance > maxSkip)
                {
                    this.Skip(maxSkip);
                    distance -= maxSkip;
                }

                if (distance > 0)
                {
                    Debug.Assert(distance < int.MaxValue, "distance exceeds int.MaxValue");
                    this.Skip(distance);
                }
            }

            this.localRemaining = parentRemaining;
        }

        public abstract string StringValue();

        public abstract long LongValue();

        public abstract SymbolToken SymbolValue();

        public abstract SymbolToken GetFieldNameSymbol();

        public abstract IntegerSize GetIntegerSize();

        public abstract ISymbolTable GetSymbolTable();

        public abstract int IntValue();

        public abstract BigInteger BigIntegerValue();

        public abstract bool BoolValue();

        public abstract Timestamp TimestampValue();

        public abstract BigDecimal DecimalValue();

        public abstract double DoubleValue();

        public abstract string[] GetTypeAnnotations();

        public abstract IEnumerable<SymbolToken> GetTypeAnnotationSymbols();

        public abstract bool HasAnnotation(string annotation);

        protected virtual bool HasNext()
        {
            if (this.eof || !this.moveNextNeeded)
            {
                return !this.eof;
            }

            try
            {
                this.MoveNextRaw();
                return !this.eof;
            }
            catch (IOException e)
            {
                throw new IonException(e);
            }
        }

        /// <summary>
        /// Read <paramref name="length"/> bytes, store results in a long.
        /// </summary>
        /// <returns>'long' representation of the value.</returns>
        /// <param name="length">number of bytes to read.</param>
        /// <remarks>If the result is less than 0, 64bit is not enough.</remarks>
        protected long ReadUlong(int length)
        {
            long ret = 0;
            int b;
            switch (length)
            {
                default:
                    throw new ArgumentOutOfRangeException(nameof(length), "length must be <=8");
                case 8:
                    if ((b = this.ReadByte()) < 0)
                    {
                        throw new UnexpectedEofException();
                    }

                    ret = (ret << 8) | (uint)b;
                    goto case 7;
                case 7:
                    if ((b = this.ReadByte()) < 0)
                    {
                        throw new UnexpectedEofException();
                    }

                    ret = (ret << 8) | (uint)b;
                    goto case 6;
                case 6:
                    if ((b = this.ReadByte()) < 0)
                    {
                        throw new UnexpectedEofException();
                    }

                    ret = (ret << 8) | (uint)b;
                    goto case 5;
                case 5:
                    if ((b = this.ReadByte()) < 0)
                    {
                        throw new UnexpectedEofException();
                    }

                    ret = (ret << 8) | (uint)b;
                    goto case 4;
                case 4:
                    if ((b = this.ReadByte()) < 0)
                    {
                        throw new UnexpectedEofException();
                    }

                    ret = (ret << 8) | (uint)b;
                    goto case 3;
                case 3:
                    if ((b = this.ReadByte()) < 0)
                    {
                        throw new UnexpectedEofException();
                    }

                    ret = (ret << 8) | (uint)b;
                    goto case 2;
                case 2:
                    if ((b = this.ReadByte()) < 0)
                    {
                        throw new UnexpectedEofException();
                    }

                    ret = (ret << 8) | (uint)b;
                    goto case 1;
                case 1:
                    if ((b = this.ReadByte()) < 0)
                    {
                        throw new UnexpectedEofException();
                    }

                    ret = (ret << 8) | (uint)b;
                    goto case 0;
                case 0:
                    break;
            }

            return ret;
        }

        protected BigDecimal ReadBigDecimal(int length)
        {
            if (length == 0)
            {
                return new BigDecimal(0m);
            }

            var saveLimit = this.localRemaining - length;
            this.localRemaining = length;

            this.ReadVarInt(out var exponent);

            // We care about the scale here
            exponent = -exponent;

            BigInteger mag;
            var negative = false;
            if (this.localRemaining == 0)
            {
                mag = BigInteger.Zero;
            }
            else
            {
                var bytes = new byte[this.localRemaining];
                this.ReadAll(bytes, this.localRemaining);

                if ((bytes[0] & 0b_1000_0000) > 0)
                {
                    // Value is negative
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

            this.localRemaining = saveLimit;
            if (negative && mag == 0)
            {
                return BigDecimal.NegativeZero(exponent);
            }

            return new BigDecimal(mag, exponent);
        }

        protected decimal ReadDecimal(int length)
        {
            if (length == 0)
            {
                return 0m;
            }

            var saveLimit = this.localRemaining - length;
            this.localRemaining = length;

            this.ReadVarInt(out var exponent);
            if (exponent > 0)
            {
                // Meaning that mantissa is zero
                if (this.localRemaining == 0)
                {
                    return 0m;
                }

                throw new IonException($"Exponent should be <= 0: {exponent}");
            }

            // We care about the scale here
            exponent = -exponent;

            decimal dec;

            if (exponent > 28)
            {
                throw new OverflowException($"Decimal exponent scale {exponent} is not supported");
            }

            if (this.localRemaining > sizeof(int) * 3)
            {
                throw new OverflowException($"Decimal mantissa size {this.localRemaining} is not supported");
            }

            if (this.localRemaining == 0)
            {
                dec = new decimal(0, 0, 0, false, (byte)exponent);
            }
            else
            {
                var mantissaSize = this.localRemaining;
                Span<byte> mantissaBytes = stackalloc byte[sizeof(int) * 3];
                this.ReadAll(mantissaBytes, this.localRemaining);

                var isNegative = (mantissaBytes[0] & 0b1000_0000) > 0;
                mantissaBytes[0] &= 0x7F;

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
                    Debug.Assert(offset <= sizeof(int), "offset is greater than int size");
                    high = ReadBigEndian(mantissaBytes.Slice(0, offset));
                }

                dec = new decimal(low, mid, high, isNegative, (byte)exponent);
            }

            this.localRemaining = saveLimit;
            return dec;
        }

        protected Timestamp ReadTimeStamp(int length)
        {
            Debug.Assert(length > 0, "length is not greater than 0");

            int month = 0, day = 0, hour = 0, minute = 0, second = 0;
            decimal frac = 0;

            var saveLimit = this.localRemaining - length;
            this.localRemaining = length; // > 0

            var offsetKnown = this.ReadVarInt(out var offset);
            var precision = Timestamp.Precision.Year;
            var year = this.ReadVarUint();
            if (this.localRemaining > 0)
            {
                month = this.ReadVarUint();
                precision = Timestamp.Precision.Month;
                if (this.localRemaining > 0)
                {
                    day = this.ReadVarUint();
                    precision = Timestamp.Precision.Day;

                    // Now we look for hours and minutes
                    if (this.localRemaining > 0)
                    {
                        hour = this.ReadVarUint();
                        minute = this.ReadVarUint();
                        precision = Timestamp.Precision.Minute;
                        if (this.localRemaining > 0)
                        {
                            second = this.ReadVarUint();
                            precision = Timestamp.Precision.Second;
                            if (this.localRemaining > 0)
                            {
                                // now we read in our actual "milliseconds since the epoch"
                                frac = this.ReadDecimal(this.localRemaining);
                            }
                        }
                    }
                }
            }

            this.localRemaining = saveLimit;

            DateTimeKind kind = this.GetOffsetKind(precision, offsetKnown, ref offset);
            return new Timestamp(year, month, day, hour, minute, second, offset, frac, precision, kind);
        }

        /// <summary>
        /// Read <paramref name="length"/> bytes into a <see cref="BigInteger"/>.
        /// </summary>
        /// <returns>The big integer.</returns>
        /// <param name="length">Number of bytes.</param>
        /// <param name="isNegative">Sign of the value.</param>
        protected BigInteger ReadBigInteger(int length, bool isNegative)
        {
            // TODO: Improve this
            if (length == 0)
            {
                return BigInteger.Zero;
            }

            var bytes = new byte[length];
            this.ReadAll(bytes, length);
            Array.Reverse(bytes);
            var bigInt = new BigInteger(bytes);
            return isNegative ? BigInteger.Negate(bigInt) : bigInt;
        }

        /// <summary>
        /// Read <paramref name="length"/> bytes into a float.
        /// </summary>
        /// <returns>The float.</returns>
        /// <param name="length">Length.</param>
        protected double ReadFloat(int length)
        {
            if (length == 0)
            {
                return 0;
            }

            if (length != 4 && length != 8)
            {
                throw new IonException($"Float length must be 0|4|8, length is {length}");
            }

            var bits = this.ReadUlong(length);
            return length == 4 ? BitConverterEx.Int32BitsToSingle((int)bits) : BitConverterEx.Int64BitsToDouble(bits);
        }

        /// <summary>
        /// Read the string value at the current position (and advance the stream by <paramref name="length"/>.
        /// </summary>
        /// <param name="length">Length of the string representation in bytes.</param>
        /// <returns>Read string.</returns>
        protected string ReadString(int length) => length <= BinaryConstants.ShortStringLength
            ? this.ReadShortString(length)
            : this.ReadLongString(length);

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

        private void ClearValue()
        {
            this.Annotations.Clear();
            this.valueType = IonType.None;
            this.valueTid = -1;
            this.valueIsNull = false;
            this.valueVariant.Clear();
            this.valueFieldId = SymbolToken.UnknownSid;
            this.hasSymbolTableAnnotation = false;
        }

        private void MoveNextRaw()
        {
            this.ClearValue();
            while (this.valueTid == -1 && !this.eof)
            {
                switch (this.state)
                {
                    default:
                        throw new IonException("should not happen");
                    case State.BeforeField:
                        Debug.Assert(this.valueFieldId == SymbolToken.UnknownSid, "valueFieldId is not UnkownSid");
                        this.valueFieldId = this.ReadFieldId();
                        if (this.valueFieldId == BinaryConstants.Eof)
                        {
                            // TODO: EOF in the middle of a struct
                            this.eof = true;
                            this.state = State.Eof;
                            break;
                        }

                        // fall through, continue to read tid
                        goto case State.BeforeTid;
                    case State.BeforeTid:
                        this.state = State.BeforeValue;
                        this.valueTid = this.ReadTypeId();
                        if (this.valueTid == BinaryConstants.Eof)
                        {
                            this.state = State.Eof;
                            this.eof = true;
                        }
                        else if (this.valueTid == BinaryConstants.TidNopPad)
                        {
                            // skips size of pad and resets State machine
                            this.Skip(this.valueLength);
                            this.ClearValue();
                        }
                        else if (this.valueTid == BinaryConstants.TidTypedecl)
                        {
                            // BinaryVersionMarker Tid happens to be Typedecl
                            if (this.valueLength == BinaryVersionMarkerLen)
                            {
                                Debug.Assert(this.valueTid == BinaryVersionMarkerTid, "valueId does not match BinaryVersionMarkerTId");

                                // This isn't valid for any type descriptor except the first byte
                                // of a 4 byte version marker, so lets read the rest
                                this.LoadVersionMarker();
                                this.valueType = IonType.Symbol;
                            }
                            else
                            {
                                // if it's not a bvm then it's an ordinary annotated value
                                this.valueType = this.LoadAnnotationsGotoValueType();
                            }
                        }
                        else
                        {
                            this.valueType = GetIonTypeFromCode(this.valueTid);
                        }

                        break;
                    case State.BeforeValue:
                        this.Skip(this.valueLength);
                        goto case State.AfterValue;
                    case State.AfterValue:
                        this.state = this.IsInStruct ? State.BeforeField : State.BeforeTid;
                        break;
                    case State.Eof:
                        break;
                }
            }

            this.moveNextNeeded = false;
        }

        private void LoadVersionMarker()
        {
            if (this.ReadByte() != 0x01)
            {
                throw new IonException("Invalid binary format");
            }

            if (this.ReadByte() != 0x00)
            {
                throw new IonException("Invalid binary format");
            }

            if (this.ReadByte() != 0xea)
            {
                throw new IonException("Invalid binary format");
            }

            // 4 byte version marker - make it look like the symbol $ion_1_0
            this.valueTid = BinaryConstants.TidSymbol;
            this.valueLength = 0;
            this.valueVariant.IntValue = SystemSymbols.Ion10Sid;
            this.valueIsNull = false;
            this.valueLobReady = false;
            this.valueFieldId = SymbolToken.UnknownSid;
            this.state = State.AfterValue;
        }

        /// <summary>
        /// Skip-ahead of the input stream in the current container.
        /// </summary>
        /// <param name="length">Maximum skip length.</param>
        /// <exception cref="ArgumentException">When <paramref name="length"/> is less than 0.</exception>
        /// <exception cref="UnexpectedEofException">If the current container has no remaining bytes.</exception>
        private void Skip(int length)
        {
            if (length < 0)
            {
                throw new ArgumentException(nameof(length));
            }

            if (this.localRemaining == NoLimit)
            {
                for (var i = 0; i < length; i++)
                {
                    this.input.ReadByte();
                }

                return;
            }

            if (length > this.localRemaining)
            {
                if (this.localRemaining < 1)
                {
                    throw new UnexpectedEofException();
                }

                length = this.localRemaining;
            }

            for (var i = 0; i < length; i++)
            {
                this.input.ReadByte();
            }

            this.localRemaining -= length;
        }

        private int ReadFieldId() => this.ReadVarUintOrEof(out var i) < 0 ? BinaryConstants.Eof : i;

        /// <summary>
        /// Read the TID bytes.
        /// </summary>
        /// <returns>Tid (type code).</returns>
        /// <exception cref="IonException">If invalid states occurs.</exception>
        private int ReadTypeId()
        {
            var tdRead = this.ReadByte();
            if (tdRead < 0)
            {
                return BinaryConstants.Eof;
            }

            var tid = BinaryConstants.GetTypeCode(tdRead);
            if (tid == BinaryConstants.TidClob)
            {
                Console.WriteLine("trouble");
            }

            var len = BinaryConstants.GetLowNibble(tdRead);
            if (tid == BinaryConstants.TidNull && len != BinaryConstants.LnIsNull)
            {
                if (len == BinaryConstants.LnIsVarLen)
                {
                    len = this.ReadVarUint();
                }

                this.state = this.IsInStruct ? State.BeforeField : State.BeforeTid;
                tid = BinaryConstants.TidNopPad;
            }
            else if (len == BinaryConstants.LnIsVarLen)
            {
                len = this.ReadVarUint();
            }
            else if (tid == BinaryConstants.TidNull)
            {
                this.valueIsNull = true;
                len = 0;
                this.state = State.AfterValue;
            }
            else if (len == BinaryConstants.LnIsNull)
            {
                this.valueIsNull = true;
                len = 0;
                this.state = State.AfterValue;
            }
            else if (tid == BinaryConstants.TidBoolean)
            {
                switch (len)
                {
                    default:
                        throw new IonException("Tid is bool but len is not null|true|false");
                    case BinaryConstants.LnBooleanTrue:
                        this.valueIsTrue = true;
                        break;
                    case BinaryConstants.LnBooleanFalse:
                        this.valueIsTrue = false;
                        break;
                }

                len = 0;
                this.state = State.AfterValue;
            }
            else if (tid == BinaryConstants.TidStruct)
            {
                this.structIsOrdered = len == 1;
                if (this.structIsOrdered)
                {
                    len = this.ReadVarUint();
                }
            }

            this.valueTid = tid;
            this.valueLength = len;
            return tid;
        }

        private int ReadVarUint()
        {
            var ret = 0;
            for (var i = 0; i < 5; i++)
            {
                var b = this.ReadByte();
                if (b < 0)
                {
                    throw new UnexpectedEofException();
                }

                ret = (ret << 7) | (b & 0x7F);
                if ((b & 0x80) != 0)
                {
                    goto Done;
                }
            }

            // If we get here we have more bits that we have room for
            throw new OverflowException($"VarUint overflow at, current fieldname {this.CurrentFieldName}");

            Done:
            return ret;
        }

        /// <summary>
        /// Try to read a VarUint or returns EOF.
        /// </summary>
        /// <param name="output">Out to store the read int.</param>
        /// <returns>Number of bytes read, or EOF.</returns>
        /// <exception cref="IonException">When unexpected EOF occurs.</exception>
        /// <exception cref="OverflowException">If the int does not self-limit.</exception>
        private int ReadVarUintOrEof(out int output)
        {
            output = 0;
            int b;
            if ((b = this.ReadByte()) < 0)
            {
                return BinaryConstants.Eof;
            }

            output = (output << 7) | (b & 0x7F);
            var bn = 1;
            if ((b & 0x80) != 0)
            {
                goto Done;
            }

            // Try reading for up to 4 more bytes
            for (var i = 0; i < 4; i++)
            {
                if ((b = this.ReadByte()) < 0)
                {
                    throw new UnexpectedEofException();
                }

                output = (output << 7) | (b & 0x7F);
                bn++;
                if ((b & 0x80) != 0)
                {
                    goto Done;
                }
            }

            // If we get here we have more bits that we have room for
            throw new OverflowException($"VarUint overflow, fieldname {this.CurrentFieldName}");

            Done:
            return bn;
        }

        /// <summary>
        /// Read a var-int.
        /// </summary>
        /// <param name="val">read output is set here.</param>
        /// <returns>False to mean -0 => unknown value.</returns>
        private bool ReadVarInt(out int val)
        {
            val = 0;
            var isNegative = false;
            var b = this.ReadByte();
            if (b < 0)
            {
                throw new UnexpectedEofException();
            }

            if ((b & 0x40) != 0)
            {
                isNegative = true;
            }

            val = b & 0x3F;
            if ((b & 0x80) != 0)
            {
                goto Done;
            }

            for (var i = 0; i < 4; i++)
            {
                if ((b = this.ReadByte()) < 0)
                {
                    throw new UnexpectedEofException();
                }

                val = (val << 7) | (b & 0x7F);
                if ((b & 0x80) != 0)
                {
                    goto Done;
                }
            }

            // Here means overflow
            throw new OverflowException();

            Done:

            // Non-negative, return now
            if (!isNegative)
            {
                return true;
            }

            // Negative zero -0
            if (val == 0)
            {
                return false;
            }

            // Otherwise just return the negation
            val = -val;
            return true;
        }

        /// <summary>
        /// Load the annotations of the current value into.
        /// </summary>
        private void LoadAnnotations(int annotLength)
        {
            // The java impl allows skipping the annotations so we can read it even if
            // state == AfterValue. We don't allow that here
            if (this.state != State.BeforeValue)
            {
                throw new InvalidOperationException("Value is not ready");
            }

            // Reset the annotation list
            this.Annotations.Clear();

            int l;
            while (annotLength > 0 && (l = this.ReadVarUintOrEof(out var a)) != BinaryConstants.Eof)
            {
                annotLength -= l;
                if (a == SystemSymbols.IonSymbolTableSid)
                {
                    this.hasSymbolTableAnnotation = true;
                }

                this.Annotations.Add(a);
            }
        }

        /// <summary>
        /// This method will read the annotations, and load them if requested.
        /// <para/> Then it will skip to the value.
        /// </summary>
        /// <returns>Type of the value.</returns>
        private IonType LoadAnnotationsGotoValueType()
        {
            // Values can be wrapped by annotations http://amzn.github.io/ion-docs/docs/binary.html#annotations
            // This is invoked when we get a typedecl tid, which means there are potentially annotations
            // depending on the options we might load them or not, the default should be not to load them
            // in which case we'll just go through to the wrapped value
            // There is no save point here, so this either loads the annotations, or it doesnt
            var annotLength = this.ReadVarUint();
            this.LoadAnnotations(annotLength);

            // This will both get the type id and it will reset the
            // length as well (over-writing the len + annotations value
            // that is there now, before the call)
            this.valueTid = this.ReadTypeId();
            if (this.valueTid == BinaryConstants.TidNopPad)
            {
                throw new IonException("NOP padding is not allowed within annotation wrappers");
            }

            if (this.valueTid == BinaryConstants.Eof)
            {
                throw new UnexpectedEofException();
            }

            if (this.valueTid == BinaryConstants.TidTypedecl)
            {
                throw new IonException("An annotation wrapper may not contain another annotation wrapper.");
            }

            var valueType = GetIonTypeFromCode(this.valueTid);
            return valueType;
        }

        private string ReadShortString(int length)
        {
            Span<byte> alloc = stackalloc byte[BinaryConstants.ShortStringLength];
            this.ReadAll(alloc, length);
            ReadOnlySpan<byte> readOnlySpan = alloc;
            var strValue = Encoding.UTF8.GetString(readOnlySpan.Slice(0, length));
            return strValue;
        }

        private string ReadLongString(int length)
        {
            var alloc = ArrayPool<byte>.Shared.Rent(length);
            this.ReadAll(new ArraySegment<byte>(alloc, 0, length), length);

            var strValue = Encoding.UTF8.GetString(alloc, 0, length);
            ArrayPool<byte>.Shared.Return(alloc);
            return strValue;
        }

        /// <summary>
        /// Read all <paramref name="length"/> bytes into the buffer.
        /// </summary>
        /// <param name="bufferSpan">Span buffer.</param>
        /// <param name="length">Amount of bytes to read.</param>
        private void ReadAll(Span<byte> bufferSpan, int length)
        {
            Debug.Assert(length <= bufferSpan.Length, "length is greater than bufferSpan.Length");
            while (length > 0)
            {
                var amount = this.input.Read(bufferSpan.Slice(0, length));
                length -= amount;
                bufferSpan = bufferSpan.Slice(amount);
                this.localRemaining -= amount;
            }
        }

        private int ReadByte()
        {
            if (this.localRemaining != NoLimit)
            {
                if (this.localRemaining < 1)
                {
                    return BinaryConstants.Eof;
                }

                this.localRemaining--;
            }

            return this.input.ReadByte();
        }

        private int ReadBytesIntoBuffer(Span<byte> buffer, int length)
        {
            if (this.localRemaining == NoLimit)
            {
                return this.input.Read(buffer);
            }

            if (length > this.localRemaining)
            {
                if (this.localRemaining < 1)
                {
                    throw new UnexpectedEofException();
                }
            }

            var bytesRead = this.input.Read(buffer);
            this.localRemaining -= bytesRead;
            return bytesRead;
        }

        private DateTimeKind GetOffsetKind(Timestamp.Precision precision, bool offsetKnown, ref int offset)
        {
            if (precision < Timestamp.Precision.Minute)
            {
                offset = 0;
                return DateTimeKind.Unspecified;
            }
            else
            {
                return offsetKnown
                    ? offset == 0 ? DateTimeKind.Utc : DateTimeKind.Local
                    : DateTimeKind.Unspecified;
            }
        }
    }
}
