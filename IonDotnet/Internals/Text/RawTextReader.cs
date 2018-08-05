using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using IonDotnet.Conversions;
using IonDotnet.Systems;

// ReSharper disable ImpureMethodCallOnReadonlyValueField

namespace IonDotnet.Internals.Text
{
    internal abstract class RawTextReader : IIonReader
    {
        #region States

        private const int StateBeforeAnnotationDatagram = 0;
        private const int StateBeforeAnnotationContained = 1;
        private const int StateBeforeAnnotationSexp = 2;
        private const int StateBeforeFieldName = 3;
        private const int StateBeforeValueContent = 4;
        private const int StateBeforeValueContentSexp = 5;
        private const int StateInLongString = 6;
        private const int StateInClobDoubleQuotedContent = 7;
        private const int StateInClobTripleQuotedContent = 8;
        private const int StateInBlobContent = 9;
        private const int StateAfterValueContents = 10;
        private const int StateEof = 11;
        private const int StateMax = 11;

        #endregion

        #region Actions

        private const short ActionNotDefined = 0;
        private const short ActionLoadFieldName = 1;
        private const short ActionLoadAnnotation = 2;
        private const short ActionStartStruct = 3;
        private const short ActionStartList = 4;
        private const short ActionStartSexp = 5;
        private const short ActionStartLob = 6;
        private const short ActionLoadScalar = 8;
        private const short ActionPlusInf = 9;
        private const short ActionMinusInf = 10;
        private const short ActionEatComma = 11; // if this is unnecessary (because load_scalar handle it) we don't need "after_value"
        private const short ActionFinishContainer = 12;
        private const short ActionFinishLob = 13;
        private const short ActionFinishDatagram = 14;
        private const short ActionEof = 15;

        private static readonly short[,] TransitionActions = MakeTransitionActionArray();

        private static short[,] MakeTransitionActionArray()
        {
            var actions = new short[StateMax + 1, TextConstants.TokenMax + 1];

            actions[StateBeforeAnnotationDatagram, TextConstants.TokenEof] = ActionFinishDatagram;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenUnknownNumeric] = ActionLoadScalar;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenInt] = ActionLoadScalar;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenBinary] = ActionLoadScalar;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenHex] = ActionLoadScalar;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenDecimal] = ActionLoadScalar;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenFloat] = ActionLoadScalar;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenFloatInf] = ActionPlusInf;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenFloatMinusInf] = ActionMinusInf;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenTimestamp] = ActionLoadScalar;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenStringDoubleQuote] = ActionLoadScalar;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenStringTripleQuote] = ActionLoadScalar;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenSymbolIdentifier] = ActionLoadAnnotation;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenSymbolQuoted] = ActionLoadAnnotation;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenOpenParen] = ActionStartSexp;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenOpenBrace] = ActionStartStruct;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenOpenSquare] = ActionStartList;
            actions[StateBeforeAnnotationDatagram, TextConstants.TokenOpenDoubleBrace] = ActionStartLob;

            // both before_annotation and after_annotation are essentially the same as
            // BOF (after_annotation can't accept EOF as valid however)
            for (int i = 0, tc = TextConstants.TokenMax + 1; i < tc; i++)
            {
                actions[StateBeforeAnnotationContained, i] = actions[StateBeforeAnnotationDatagram, i];
                actions[StateBeforeAnnotationSexp, i] = actions[StateBeforeAnnotationDatagram, i];
                actions[StateBeforeValueContent, i] = actions[StateBeforeAnnotationDatagram, i];
                actions[StateBeforeValueContentSexp, i] = actions[StateBeforeAnnotationDatagram, i];
            }

            // now patch up the differences between these 4 states handling of tokens vs before_annotation_datagram
            actions[StateBeforeAnnotationContained, TextConstants.TokenEof] = ActionNotDefined;
            actions[StateBeforeAnnotationContained, TextConstants.TokenCloseParen] = ActionFinishContainer;
            actions[StateBeforeAnnotationContained, TextConstants.TokenCloseBrace] = ActionFinishContainer;
            actions[StateBeforeAnnotationContained, TextConstants.TokenCloseSquare] = ActionFinishContainer;

            actions[StateBeforeAnnotationSexp, TextConstants.TokenEof] = ActionNotDefined;
            actions[StateBeforeAnnotationSexp, TextConstants.TokenSymbolOperator] = ActionLoadScalar;
            actions[StateBeforeAnnotationSexp, TextConstants.TokenDot] = ActionLoadScalar;
            actions[StateBeforeAnnotationSexp, TextConstants.TokenCloseParen] = ActionFinishContainer;
            actions[StateBeforeAnnotationSexp, TextConstants.TokenCloseBrace] = ActionFinishContainer;
            actions[StateBeforeAnnotationSexp, TextConstants.TokenCloseSquare] = ActionFinishContainer;

            actions[StateBeforeValueContent, TextConstants.TokenEof] = ActionNotDefined;
            actions[StateBeforeValueContent, TextConstants.TokenSymbolIdentifier] = ActionLoadScalar;
            actions[StateBeforeValueContent, TextConstants.TokenSymbolQuoted] = ActionLoadScalar;

            actions[StateBeforeValueContentSexp, TextConstants.TokenEof] = ActionNotDefined;
            actions[StateBeforeValueContentSexp, TextConstants.TokenSymbolIdentifier] = ActionLoadScalar;
            actions[StateBeforeValueContentSexp, TextConstants.TokenSymbolQuoted] = ActionLoadScalar;
            actions[StateBeforeValueContentSexp, TextConstants.TokenSymbolOperator] = ActionLoadScalar;

            actions[StateBeforeFieldName, TextConstants.TokenEof] = 0;
            actions[StateBeforeFieldName, TextConstants.TokenSymbolIdentifier] = ActionLoadFieldName;
            actions[StateBeforeFieldName, TextConstants.TokenSymbolQuoted] = ActionLoadFieldName;
            actions[StateBeforeFieldName, TextConstants.TokenStringDoubleQuote] = ActionLoadFieldName;
            actions[StateBeforeFieldName, TextConstants.TokenStringTripleQuote] = ActionLoadFieldName;
            actions[StateBeforeFieldName, TextConstants.TokenCloseParen] = ActionFinishContainer;
            actions[StateBeforeFieldName, TextConstants.TokenCloseBrace] = ActionFinishContainer;
            actions[StateBeforeFieldName, TextConstants.TokenCloseSquare] = ActionFinishContainer;

            // after a value we'll either see a separator (like ',') or a containers closing token. If we're not in a container
            // (i.e. we're at the top level) then this isn't the state we should be in.  We'll be in StateBeforeAnnotationDatagram
            actions[StateAfterValueContents, TextConstants.TokenComma] = ActionEatComma;
            actions[StateAfterValueContents, TextConstants.TokenCloseParen] = ActionFinishContainer;
            actions[StateAfterValueContents, TextConstants.TokenCloseBrace] = ActionFinishContainer;
            actions[StateAfterValueContents, TextConstants.TokenCloseSquare] = ActionFinishContainer;

            // the three "in_<lob>" value states have to be handled
            // specially, they can only scan forward to the end of
            // the content on next, or read content for the user otherwise
            actions[StateInClobDoubleQuotedContent, TextConstants.TokenCloseBrace] = ActionFinishLob;
            actions[StateInClobTripleQuotedContent, TextConstants.TokenCloseBrace] = ActionFinishLob;
            actions[StateInBlobContent, TextConstants.TokenCloseBrace] = ActionFinishLob;

            // the eof action exists because finishing an unread value can place the scanner just before
            // the input stream eof and set the current state to eof - in which case we just need to return eof
            for (int i = 0, tc = TextConstants.TokenMax + 1; i < tc; i++)
            {
                actions[StateEof, i] = ActionEof;
            }

            return actions;
        }

        #endregion

        protected readonly StringBuilder _valueBuffer;
        protected ValueVariant _v;
        protected readonly RawTextScanner _scanner;
        private readonly List<SymbolToken> _annotations = new List<SymbolToken>();

        private int _state;
        private bool _eof;
        private bool _valueBufferLoaded;

        protected IonType _valueType;
        private bool _containerIsStruct; // helper bool's set on push and pop and used
        private bool _containerProhibitsCommas; // frequently during state transitions actions
        private bool _hasNextCalled;
        private string _fieldName;
        private int _fieldNameSid = SymbolToken.UnknownSid;

        private readonly ContainerStack _containerStack;

        protected RawTextReader(TextStream input, IonType parent = IonType.None)
        {
            _state = GetStateAtContainerStart(parent);
            _valueBuffer = new StringBuilder();
            _scanner = new RawTextScanner(input);
            _eof = false;
            _hasNextCalled = false;
            _containerStack = new ContainerStack(this, 6);
        }

        protected void ClearValueBuffer()
        {
            _valueBuffer.Clear();
        }

        private bool HasNext()
        {
            if (_hasNextCalled || _eof)
                return _eof != true;

            FinishValue();
            ClearValue();
            ParseNext();

            _hasNextCalled = true;
            return _eof != true;
        }

        private void ClearValue()
        {
            _valueType = IonType.None;
            //TODO lob
            ClearValueBuffer();
            _annotations.Clear();
            ClearFieldName();
            _v.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearFieldName()
        {
            _fieldName = null;
            _fieldNameSid = SymbolToken.UnknownSid;
        }

        private void ParseNext()
        {
            int temp_state;
            // TODO: there's a better way to do this
            var trailing_whitespace = false;
            StringBuilder sb;

            var token = _scanner.NextToken();
            while (true)
            {
                int action = TransitionActions[_state, token];
                switch (action)
                {
                    default:
                        throw new InvalidTokenException(token);
                    case ActionNotDefined:
                        throw new IonException("Invalid state");
                    case ActionEof:
                        _state = StateEof;
                        _eof = true;
                        return;
                    case ActionLoadFieldName:
                        if (!IsInStruct)
                            throw new IonException("Field names have to be inside struct");
                        FinishValue();
                        LoadTokenContents(token);
                        var symtok = ParseSymbolToken(_valueBuffer, token);
                        SetFieldName(symtok);
                        ClearValueBuffer();
                        token = _scanner.NextToken();
                        if (token != TextConstants.TokenColon)
                            throw new InvalidTokenException("Field name has to be followed by a colon");
                        _scanner.MarkTokenFinished();
                        _state = StateBeforeAnnotationContained;
                        token = _scanner.NextToken();
                        break;
                    case ActionLoadScalar:
                        if (token == TextConstants.TokenSymbolIdentifier)
                        {
                            throw new NotImplementedException();
                        }
                        else if (token == TextConstants.TokenDot)
                        {
                            throw new NotImplementedException();
                        }
                        else
                        {
                            // if it's not a symbol we just look at the token type
                            _valueType = TextConstants.GetIonTypeOfToken(token);
                        }

                        _state = GetStateAfterValue(_containerStack.Peek());
                        break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetFieldName(in SymbolToken symtok)
        {
            _fieldName = symtok.Text;
            _fieldNameSid = symtok.Sid;
        }

        private static SymbolToken ParseSymbolToken(StringBuilder sb, int token)
        {
            if (token == TextConstants.TokenSymbolIdentifier)
            {
                throw new NotImplementedException();
//                int kw = TextConstants.keyword(sb, 0, sb.length());
//                switch (kw)
//                {
//                    case TextConstants.KEYWORD_FALSE:
//                    case TextConstants.KEYWORD_TRUE:
//                    case TextConstants.KEYWORD_NULL:
//                    case TextConstants.KEYWORD_NAN:
//                        // keywords are not ok unless they're quoted
//                        throw new IonException($"Cannot use unquoted keyword {sb}");
//                    case TextConstants.KEYWORD_sid:
//                        text = null;
//                        sid = TextConstants.decodeSid(sb);
//                        break;
//                    default:
//                        text = sb.toString();
//                        sid = UNKNOWN_SYMBOL_ID;
//                        break;
//                }
            }

            return new SymbolToken(sb.ToString(), SymbolToken.UnknownSid);
        }

        private void FinishValue()
        {
            if (_scanner.UnfinishedToken)
            {
                _scanner.FinishToken();
                _state = GetStateAfterValue(_containerStack.Peek());
            }

            _hasNextCalled = false;
        }

        public IonType MoveNext()
        {
            if (!HasNext())
                return IonType.None;

            if (_valueType == IonType.None && _scanner.UnfinishedToken)
            {
                LoadTokenContents(_scanner.Token);
            }

            _hasNextCalled = false;
            return _valueType;
        }

        protected void LoadTokenContents(int scannerToken)
        {
            if (_valueBufferLoaded)
                return;

            int c;
            bool clobCharsOnly;
            switch (scannerToken)
            {
                default:
                    throw new InvalidTokenException(scannerToken);
                case TextConstants.TokenUnknownNumeric:
                case TextConstants.TokenInt:
                case TextConstants.TokenBinary:
                case TextConstants.TokenHex:
                case TextConstants.TokenFloat:
                case TextConstants.TokenDecimal:
                case TextConstants.TokenTimestamp:
                    _valueType = _scanner.LoadNumber(_valueBuffer);
                    break;
                case TextConstants.TokenSymbolIdentifier:
                    _scanner.LoadSymbolIdentifier(_valueBuffer);
                    _valueType = IonType.Symbol;
                    break;
                case TextConstants.TokenSymbolOperator:
                    _scanner.LoadSymbolOperator(_valueBuffer);
                    _valueType = IonType.Symbol;
                    break;
                case TextConstants.TokenSymbolQuoted:
                    clobCharsOnly = IonType.Clob == _valueType;
                    c = _scanner.LoadSingleQuotedString(_valueBuffer, clobCharsOnly);
                    if (c == TextConstants.TokenEof)
                        throw new UnexpectedEofException();

                    _valueType = IonType.Symbol;
                    break;
                case TextConstants.TokenStringDoubleQuote:
                    clobCharsOnly = IonType.Clob == _valueType;
                    c = _scanner.LoadDoubleQuotedString(_valueBuffer, clobCharsOnly);
                    if (c == TextConstants.TokenEof)
                        throw new UnexpectedEofException();

                    _valueType = IonType.String;
                    break;
                case TextConstants.TokenStringTripleQuote:
                    clobCharsOnly = IonType.Clob == _valueType;
                    c = _scanner.LoadTripleQuotedString(_valueBuffer, clobCharsOnly);
                    if (c == TextConstants.TokenEof)
                        throw new UnexpectedEofException();

                    _valueType = IonType.String;
                    break;
            }
        }

        public void StepIn()
        {
            if (!_valueType.IsContainer())
                throw new InvalidOperationException($"Current value type {_valueType} is not a container");

            _state = GetStateAtContainerStart(_valueType);
            _containerStack.PushContainer(_valueType);
            _scanner.FinishToken();
            FinishValue();
            if (_v.TypeSet.HasFlag(ScalarType.Null))
            {
                _eof = true;
                _hasNextCalled = true;
            }

            _valueType = IonType.None;
        }

        public void StepOut()
        {
            if (CurrentDepth < 1)
                throw new InvalidOperationException("Already at outer most");

            FinishValue();
            switch (_containerStack.Peek())
            {
                default:
                    throw new IonException("Invalid state");
                case IonType.Datagram:
                    break;
                case IonType.List:
                    _scanner.SkipOverList();
                    break;
                case IonType.Struct:
                    _scanner.SkipOverStruct();
                    break;
                case IonType.Sexp:
                    _scanner.SkipOverSexp();
                    break;
            }

            _containerStack.Pop();
            FinishValue();
            ClearValue();
        }

        public int CurrentDepth
        {
            get
            {
                var top = _containerStack.Count;

                if (top == 0)
                    //not sure why this would ever happen
                    return 0;

                //subtract 1 because level '0' is the datagram
                Debug.Assert(_containerStack.First() == IonType.Datagram);
                return top - 1;
                //TODO handle nested parent
            }
        }

        public ISymbolTable GetSymbolTable()
        {
            throw new NotImplementedException();
        }

        public IonType CurrentType { get; }

        public IntegerSize GetIntegerSize()
        {
            throw new NotImplementedException();
        }

        public string CurrentFieldName
        {
            get
            {
                if (CurrentDepth == 0 && IsInStruct)
                    return null;
                if (_fieldName == null && _fieldNameSid > 0)
                    throw new UnknownSymbolException(_fieldNameSid);
                return _fieldName;
            }
        }

        public SymbolToken GetFieldNameSymbol()
        {
            throw new NotImplementedException();
        }

        public abstract bool CurrentIsNull { get; }
        public bool IsInStruct => _containerStack.Count > 0 && _containerStack.Peek() == IonType.Struct;

        public abstract bool BoolValue();

        public abstract int IntValue();

        public abstract long LongValue();

        public abstract BigInteger BigIntegerValue();

        public abstract double DoubleValue();

        public abstract decimal DecimalValue();

        public abstract Timestamp TimestampValue();

        public abstract string StringValue();

        public abstract SymbolToken SymbolValue();

        public int GetLobByteSize()
        {
            throw new NotImplementedException();
        }

        public byte[] NewByteArray()
        {
            throw new NotImplementedException();
        }

        public int GetBytes(ArraySegment<byte> buffer)
        {
            throw new NotImplementedException();
        }

        public bool TryConvertTo(Type targetType, IScalarConverter scalarConverter, out object result)
        {
            throw new NotImplementedException();
        }

        private static int GetStateAtContainerStart(IonType container)
        {
            if (container == IonType.None)
                return StateBeforeAnnotationDatagram;

            Debug.Assert(container.IsContainer());

            switch (container)
            {
                default:
                    //should not happen
                    throw new IonException($"{container} is no container");
                case IonType.Struct:
                    return StateBeforeFieldName;
                case IonType.List:
                    return StateBeforeAnnotationContained;
                case IonType.Sexp:
                    return StateBeforeAnnotationSexp;
                case IonType.Datagram:
                    return StateBeforeAnnotationDatagram;
            }
        }

        private static int GetStateAfterValue(IonType currentContainerType)
        {
            //TODO handle nested parent
//            if (_nesting_parent != null && getDepth() == 0) {
//                state_after_scalar = STATE_EOF;
//            }

            switch (currentContainerType)
            {
                default:
                    throw new IonException($"{currentContainerType} is no container");
                case IonType.List:
                case IonType.Struct:
                    return StateAfterValueContents;
                case IonType.Sexp:
                    return StateBeforeAnnotationSexp;
                case IonType.Datagram:
                    return StateBeforeAnnotationDatagram;
            }
        }

        private static int GetStateAfterContainer(IonType newContainer)
        {
            if (newContainer == IonType.None)
                return StateBeforeAnnotationDatagram;
            Debug.Assert(newContainer.IsContainer());

            //TODO handle the case for nesting parent that returns eof when its scope ends
//            if (_nestingparent != None && CurrentDepth == 0) {
//                new_state = STATE_EOF;
//            }

            switch (newContainer)
            {
                default:
                    //should not happen
                    throw new IonException($"{newContainer} is no container");
                case IonType.Struct:
                case IonType.List:
                    return StateAfterValueContents;
                case IonType.Sexp:
                    return StateBeforeAnnotationSexp;
                case IonType.Datagram:
                    return StateBeforeAnnotationDatagram;
            }
        }

        private class ContainerStack
        {
            private readonly RawTextReader _rawTextReader;
            private IonType[] _array;

            public ContainerStack(RawTextReader rawTextReader, int initialCapacity)
            {
                Debug.Assert(initialCapacity > 0);
                _rawTextReader = rawTextReader;
                _array = new IonType[initialCapacity];
            }

            public void PushContainer(IonType containerType)
            {
                EnsureCapacity(Count);
                _array[Count] = containerType;
                SetContainerFlags(containerType);

                Count++;
            }

            public IonType Peek()
            {
                if (Count == 0)
                    throw new IndexOutOfRangeException();
                return _array[Count - 1];
            }

            public IonType Pop()
            {
                if (Count == 0)
                    throw new IndexOutOfRangeException();
                var ret = _array[--Count];

                //TODO should we do book keeping here?
                _rawTextReader._eof = false;
                _rawTextReader._hasNextCalled = false;
                var topState = _array[Count - 1];
                SetContainerFlags(topState);
                _rawTextReader._state = GetStateAfterContainer(topState);

                return ret;
            }

            public void Clear() => Count = 0;

            public IonType First() => _array[0];

            public int Count { get; private set; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetContainerFlags(IonType type)
            {
                switch (type)
                {
                    default:
                        throw new IonException($"{type} is no container");
                    case IonType.Struct:
                        _rawTextReader._containerIsStruct = true;
                        _rawTextReader._containerProhibitsCommas = false;
                        return;
                    case IonType.List:
                        _rawTextReader._containerIsStruct = false;
                        _rawTextReader._containerProhibitsCommas = false;
                        return;
                    case IonType.Datagram:
                        _rawTextReader._containerIsStruct = false;
                        _rawTextReader._containerProhibitsCommas = true;
                        return;
                    case IonType.Sexp:
                        _rawTextReader._containerIsStruct = false;
                        _rawTextReader._containerProhibitsCommas = false;
                        return;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void EnsureCapacity(int forIndex)
            {
                if (forIndex < _array.Length) return;
                //resize
                Array.Resize(ref _array, _array.Length * 2);
            }
        }
    }
}
