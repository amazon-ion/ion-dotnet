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

        private readonly ContainerStack _containerStack;
        protected readonly StringBuilder _valueBuffer;
        protected readonly TextScanner _scanner;
        protected readonly List<SymbolToken> _annotations = new List<SymbolToken>();

        protected ValueVariant _v;
        private int _state;
        protected bool _eof;
        protected int _valueKeyword;

        protected IonType _valueType;
        private bool _tokenContentLoaded;
        private bool _containerIsStruct; // helper bool's set on push and pop and used
        private bool _containerProhibitsCommas; // frequently during state transitions actions
        protected bool _hasNextCalled;
        protected string _fieldName;
        protected int _fieldNameSid = SymbolToken.UnknownSid;


        protected int _lobToken;
        protected int _lobValuePosition;
        protected byte[] _lobBuffer;

        protected RawTextReader(TextStream input)
        {
            _state = GetStateAtContainerStart(IonType.Datagram);
            _valueBuffer = new StringBuilder();
            _scanner = new TextScanner(input);
            _eof = false;
            _valueType = IonType.None;
            _hasNextCalled = false;
            _containerStack = new ContainerStack(this, 6);
            _containerStack.PushContainer(IonType.Datagram);
        }

        protected void ClearValueBuffer()
        {
            _tokenContentLoaded = false;
            _valueBuffer.Clear();
        }

        protected virtual bool HasNext()
        {
            if (_hasNextCalled || _eof)
                return _eof != true;

            FinishValue();
            ClearValue();
            ParseNext();

            _hasNextCalled = true;
            return !_eof;
        }

        private void ClearValue()
        {
            _valueType = IonType.None;
            ClearValueBuffer();
            _annotations.Clear();
            ClearFieldName();
            _v.Clear();
            _lobValuePosition = 0;
            _lobBuffer = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearFieldName()
        {
            _fieldName = null;
            _fieldNameSid = SymbolToken.UnknownSid;
        }

        private void ParseNext()
        {
            var trailingWhitespace = false;

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
                    case ActionLoadAnnotation:
                        LoadTokenContents(token);
                        trailingWhitespace = _scanner.SkipWhiteSpace();
                        if (!_scanner.TrySkipDoubleColon())
                        {
                            _state = GetStateAfterAnnotation(_state, _containerStack.Peek());
                            break;
                        }

                        var sym = ParseSymbolToken(_valueBuffer, token);
                        _annotations.Add(sym);
                        ClearValueBuffer();
                        _scanner.MarkTokenFinished();
                        // Consumed the annotation, move on.
                        // note: that peekDoubleColon() consumed the two colons
                        // so nextToken won't see them
                        token = _scanner.NextToken();
                        switch (token)
                        {
                            case TextConstants.TokenSymbolIdentifier:
                            case TextConstants.TokenSymbolQuoted:
                                // This may be another annotation, so stay in this state
                                // and come around the horn again to check it out.
                                break;
                            default:
                                // we leave the error handling to the transition
                                _state = GetStateAfterAnnotation(_state, _containerStack.Peek());
                                break;
                        }

                        break;
                    case ActionStartStruct:
                        _valueType = IonType.Struct;
                        _state = StateBeforeFieldName;
                        return;
                    case ActionStartList:
                        _valueType = IonType.List;
                        _state = StateBeforeAnnotationContained;
                        return;
                    case ActionStartSexp:
                        _valueType = IonType.Sexp;
                        _state = StateBeforeAnnotationSexp;
                        return;
                    case ActionStartLob:
                        switch (_scanner.PeekLobStartPunctuation())
                        {
                            case TextConstants.TokenStringDoubleQuote:
                                _lobToken = TextConstants.TokenStringDoubleQuote;
                                _valueType = IonType.Clob;
                                break;
                            case TextConstants.TokenStringTripleQuote:
                                _lobToken = TextConstants.TokenStringTripleQuote;
                                _valueType = IonType.Clob;
                                break;
                            default:
                                _state = StateInBlobContent;
                                _lobToken = TextConstants.TokenOpenDoubleBrace;
                                _valueType = IonType.Blob;
                                break;
                        }

                        return;
                    case ActionLoadScalar:
                        if (token == TextConstants.TokenSymbolIdentifier)
                        {
                            LoadTokenContents(token);
                            //token has been wholy loaded
                            _scanner.MarkTokenFinished();

                            _valueKeyword = TextConstants.GetKeyword(_valueBuffer, 0, _valueBuffer.Length);
                            switch (_valueKeyword)
                            {
                                default:
                                    _valueType = IonType.Symbol;
                                    break;
                                case TextConstants.KeywordNull:
                                    ReadNullType(trailingWhitespace);
                                    break;
                                case TextConstants.KeywordTrue:
                                    _valueType = IonType.Bool;
                                    _v.BoolValue = true;
                                    break;
                                case TextConstants.KeywordFalse:
                                    _valueType = IonType.Bool;
                                    _v.BoolValue = false;
                                    break;
                                case TextConstants.KeywordNan:
                                    _valueType = IonType.Float;
                                    ClearValueBuffer();
                                    _v.DoubleValue = double.NaN;
                                    break;
                                case TextConstants.KeywordSid:
                                    var sid = TextConstants.DecodeSid(_valueBuffer);
                                    _v.IntValue = sid;
                                    _valueType = IonType.Symbol;
                                    break;
                            }

                            //do not clear the buffer yet because LoadTokenContents() might be called again
                        }
                        else if (token == TextConstants.TokenDot)
                        {
                            _valueType = IonType.Symbol;
                            ClearValueBuffer();
                            _v.StringValue = ".";
                        }
                        else
                        {
                            // if it's not a symbol we just look at the token type
                            _valueType = TextConstants.GetIonTypeOfToken(token);
                        }

                        _state = GetStateAfterValue(_containerStack.Peek());
                        return;
                    case ActionEatComma:
                        if (_containerProhibitsCommas)
                            throw new InvalidTokenException(',');

                        _state = _containerIsStruct ? StateBeforeFieldName : StateBeforeAnnotationContained;
                        _scanner.MarkTokenFinished();
                        token = _scanner.NextToken();
                        break;
                    case ActionFinishDatagram:
                        Debug.Assert(CurrentDepth == 0);
                        _eof = true;
                        _state = StateEof;
                        return;
                    case ActionFinishContainer:
                        _state = GetStateAfterContainer(_containerStack.Peek());
                        _eof = true;
                        return;
                    case ActionFinishLob:
                        _state = GetStateAfterValue(_containerStack.Peek());
                        return;
                    case ActionPlusInf:
                        _valueType = IonType.Float;
                        ClearValueBuffer();
                        _v.DoubleValue = double.PositiveInfinity;
                        _state = GetStateAfterValue(_containerStack.Peek());
                        return;
                    case ActionMinusInf:
                        _valueType = IonType.Float;
                        ClearValueBuffer();
                        _v.DoubleValue = double.NegativeInfinity;
                        _state = GetStateAfterValue(_containerStack.Peek());
                        return;
                }
            }
        }

        private void ReadNullType(bool trailingWhitespace)
        {
            var kwt = trailingWhitespace ? TextConstants.KeywordNone : _scanner.PeekNullTypeSymbol();
            switch (kwt)
            {
                case TextConstants.KeywordNull:
                    _valueType = IonType.Null;
                    break;
                case TextConstants.KeywordBool:
                    _valueType = IonType.Bool;
                    break;
                case TextConstants.KeywordInt:
                    _valueType = IonType.Int;
                    break;
                case TextConstants.KeywordFloat:
                    _valueType = IonType.Float;
                    break;
                case TextConstants.KeywordDecimal:
                    _valueType = IonType.Decimal;
                    break;
                case TextConstants.KeywordTimestamp:
                    _valueType = IonType.Timestamp;
                    break;
                case TextConstants.KeywordSymbol:
                    _valueType = IonType.Symbol;
                    break;
                case TextConstants.KeywordString:
                    _valueType = IonType.String;
                    break;
                case TextConstants.KeywordBlob:
                    _valueType = IonType.Blob;
                    break;
                case TextConstants.KeywordClob:
                    _valueType = IonType.Clob;
                    break;
                case TextConstants.KeywordList:
                    _valueType = IonType.List;
                    break;
                case TextConstants.KeywordSexp:
                    _valueType = IonType.Sexp;
                    break;
                case TextConstants.KeywordStruct:
                    _valueType = IonType.Struct;
                    break;
                case TextConstants.KeywordNone:
                    _valueType = IonType.Null;
                    break; // this happens when there isn't a '.' otherwise peek
                // throws the error or returns none
                default:
                    throw new IonException($"invalid keyword id ({kwt}) encountered while parsing a null");
            }

            // at this point we've consumed a dot '.' and it's preceding
            // whitespace
            // clear_value();
            _v.SetNull(_valueType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetFieldName(in SymbolToken symtok)
        {
            _fieldName = symtok.Text;
            _fieldNameSid = symtok.Sid;
        }

        private SymbolToken ParseSymbolToken(StringBuilder sb, int token)
        {
            if (token != TextConstants.TokenSymbolIdentifier)
                return new SymbolToken(sb.ToString(), SymbolToken.UnknownSid);

            var kw = TextConstants.GetKeyword(sb, 0, sb.Length);
            string text;
            int sid;
            switch (kw)
            {
                case TextConstants.KeywordFalse:
                case TextConstants.KeywordTrue:
                case TextConstants.KeywordNull:
                case TextConstants.KeywordNan:
                    // keywords are not ok unless they're quoted
                    throw new IonException($"Cannot use unquoted keyword {sb}");
                case TextConstants.KeywordSid:
                    sid = TextConstants.DecodeSid(sb);
                    text = GetSymbolTable().FindKnownSymbol(sid);
                    break;
                default:
                    text = sb.ToString();
                    sid = GetSymbolTable().FindSymbolId(text);
                    break;
            }

            return new SymbolToken(text, sid);
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

        protected IonType GetContainerType() => _containerStack.Peek();

        protected void LoadTokenContents(int scannerToken)
        {
            if (_tokenContentLoaded)
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

            _tokenContentLoaded = true;
        }

        public void StepIn()
        {
            if (!_valueType.IsContainer())
                throw new InvalidOperationException($"Current value type {_valueType} is not a container");

            _state = GetStateAtContainerStart(_valueType);
            _containerStack.PushContainer(_valueType);
            _scanner.MarkTokenFinished();

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
                    if (!_eof)
                    {
                        _scanner.SkipOverList();
                    }

                    break;
                case IonType.Struct:
                    if (!_eof)
                    {
                        _scanner.SkipOverStruct();
                    }

                    break;
                case IonType.Sexp:
                    if (!_eof)
                    {
                        _scanner.SkipOverSexp();
                    }

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

        public abstract ISymbolTable GetSymbolTable();

        public IonType CurrentType => _valueType;

        public abstract IntegerSize GetIntegerSize();

        public abstract string CurrentFieldName { get; }

        public abstract SymbolToken GetFieldNameSymbol();

        public abstract bool CurrentIsNull { get; }
        public bool IsInStruct => _containerStack.Count > 0 && _containerStack.Peek() == IonType.Struct;

        public abstract bool BoolValue();

        public abstract int IntValue();

        public abstract long LongValue();

        public abstract BigInteger BigIntegerValue();

        public abstract double DoubleValue();

        public abstract BigDecimal DecimalValue();

        public abstract Timestamp TimestampValue();

        public abstract string StringValue();

        public abstract SymbolToken SymbolValue();

        public abstract int GetLobByteSize();

        public abstract byte[] NewByteArray();

        public abstract int GetBytes(Span<byte> buffer);

        public IEnumerable<SymbolToken> GetTypeAnnotations()
        {
            if (_annotations == null)
            {
                yield break;
            }

            foreach (var a in _annotations)
            {
                if (a.Text is null && a.Sid != 0)
                {
                    var symtab = GetSymbolTable();
                    if (a.Sid < -1 || a.Sid > symtab.MaxId)
                    {
                        throw new UnknownSymbolException(a.Sid);
                    }

                    var text = symtab.FindKnownSymbol(a.Sid);
                    yield return new SymbolToken(text, a.Sid, a.ImportLocation);
                }
                else
                {
                    yield return a;
                }
            }
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

        private static int GetStateAfterAnnotation(int stateBeforeAnnotation, IonType container)
        {
            switch (stateBeforeAnnotation)
            {
                default:
                    throw new IonException($"Invalid state before annotation {stateBeforeAnnotation}");
                case StateAfterValueContents:
                    switch (container)
                    {
                        default:
                            throw new IonException($"{container} is no container");
                        case IonType.Struct:
                        case IonType.List:
                        case IonType.Datagram:
                            return StateBeforeValueContent;
                        case IonType.Sexp:
                            return StateBeforeValueContentSexp;
                    }
                case StateBeforeAnnotationDatagram:
                case StateBeforeAnnotationContained:
                    return StateBeforeValueContent;
                case StateBeforeAnnotationSexp:
                    return StateBeforeValueContentSexp;
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

            public void Pop()
            {
                if (Count == 0)
                    throw new IndexOutOfRangeException();
                Count--;

                _rawTextReader._eof = false;
                _rawTextReader._hasNextCalled = false;
                var topState = _array[Count - 1];
                SetContainerFlags(topState);
                _rawTextReader._state = GetStateAfterContainer(topState);
            }

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
