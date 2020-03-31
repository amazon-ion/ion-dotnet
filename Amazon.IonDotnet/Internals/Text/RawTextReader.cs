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

namespace Amazon.IonDotnet.Internals.Text
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Text;
    using Amazon.IonDotnet.Internals.Conversions;

    internal abstract class RawTextReader : IIonReader
    {
        protected readonly StringBuilder valueBuffer;
        protected readonly TextScanner scanner;
        protected readonly List<SymbolToken> annotations = new List<SymbolToken>();

        protected ValueVariant valueVariant;
        protected bool eof;
        protected int valueKeyword;
        protected IonType valueType;
        protected bool hasNextCalled;

        protected string fieldName;
        protected int fieldNameSid = SymbolToken.UnknownSid;

        protected int lobToken;
        protected int lobValuePosition;
        protected byte[] lobBuffer;

#pragma warning disable IDE0051 // Remove unused private members
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
#pragma warning restore IDE0051 // Remove unused private members

        private static readonly short[,] TransitionActions = MakeTransitionActionArray();

        // For any container being opened, its closing symbol should come before
        // any other closing symbol: ) ] }  eg: [2, (hi),  { a:h }, ''' abc ''']
        private readonly Stack<int> expectedContainerClosingSymbol = new Stack<int>();

        private readonly ContainerStack containerStack;

        private int state;
        private bool tokenContentLoaded;
        private bool containerIsStruct; // helper bool's set on push and pop and used
        private bool containerProhibitsCommas; // frequently during state transitions actions

        protected RawTextReader(TextStream input)
        {
            this.state = GetStateAtContainerStart(IonType.Datagram);
            this.valueBuffer = new StringBuilder();
            this.scanner = new TextScanner(input);
            this.eof = false;
            this.valueType = IonType.None;
            this.hasNextCalled = false;
            this.containerStack = new ContainerStack(this, 6);
            this.containerStack.PushContainer(IonType.Datagram);
        }

        public IonType CurrentType => this.valueType;

        public abstract string CurrentFieldName { get; }

        public abstract bool CurrentIsNull { get; }

        public bool IsInStruct => this.containerStack.Count > 0 && this.containerStack.Peek() == IonType.Struct;

        public int CurrentDepth
        {
            get
            {
                var top = this.containerStack.Count;

                if (top == 0)
                {
                    // Usually won't happen
                    return 0;
                }

                // subtract 1 because level '0' is the datagram
                Debug.Assert(this.containerStack.First() == IonType.Datagram, "First tpye in containerStack is not Datagram");
                return top - 1;
            }
        }

        public IonType MoveNext()
        {
            if (!this.HasNext())
            {
                return IonType.None;
            }

            if (this.valueType == IonType.None && this.scanner.UnfinishedToken)
            {
                this.LoadTokenContents(this.scanner.Token);
            }

            this.hasNextCalled = false;
            return this.valueType;
        }

        public void StepIn()
        {
            if (!this.valueType.IsContainer())
            {
                throw new InvalidOperationException($"Current value type {this.valueType} is not a container");
            }

            this.state = GetStateAtContainerStart(this.valueType);
            this.containerStack.PushContainer(this.valueType);
            this.scanner.MarkTokenFinished();

            this.FinishValue();
            if (this.valueVariant.TypeSet.HasFlag(ScalarType.Null))
            {
                this.eof = true;
                this.hasNextCalled = true;
            }

            this.valueType = IonType.None;
        }

        public void StepOut()
        {
            if (this.CurrentDepth < 1)
            {
                throw new InvalidOperationException("Already at outer most");
            }

            this.FinishValue();
            switch (this.containerStack.Peek())
            {
                default:
                    throw new IonException("Invalid state");
                case IonType.Datagram:
                    break;
                case IonType.List:
                    if (!this.eof)
                    {
                        this.scanner.SkipOverList();
                    }

                    break;
                case IonType.Struct:
                    if (!this.eof)
                    {
                        this.scanner.SkipOverStruct();
                    }

                    break;
                case IonType.Sexp:
                    if (!this.eof)
                    {
                        this.scanner.SkipOverSexp();
                    }

                    break;
            }

            this.containerStack.Pop();
            this.FinishValue();
            this.ClearValue();
        }

        public abstract ISymbolTable GetSymbolTable();

        public abstract IntegerSize GetIntegerSize();

        public abstract SymbolToken GetFieldNameSymbol();

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

        /// <summary>
        /// Dispose RawTextReader.
        /// </summary>
        public virtual void Dispose()
        {
            return;
        }

        public abstract string[] GetTypeAnnotations();

        public abstract IEnumerable<SymbolToken> GetTypeAnnotationSymbols();

        public abstract bool HasAnnotation(string annotation);

        protected void ClearValueBuffer()
        {
            this.tokenContentLoaded = false;
            this.valueBuffer.Clear();
        }

        protected virtual bool HasNext()
        {
            if (this.hasNextCalled || this.eof)
            {
                return this.eof != true;
            }

            this.FinishValue();
            this.ClearValue();
            this.ParseNext();

            this.hasNextCalled = true;
            return !this.eof;
        }

        protected IonType GetContainerType() => this.containerStack.Peek();

        protected void LoadTokenContents(int scannerToken)
        {
            if (this.tokenContentLoaded)
            {
                return;
            }

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
                    this.valueType = this.scanner.LoadNumber(this.valueBuffer);
                    break;
                case TextConstants.TokenSymbolIdentifier:
                    this.scanner.LoadSymbolIdentifier(this.valueBuffer);
                    this.valueType = IonType.Symbol;
                    break;
                case TextConstants.TokenSymbolOperator:
                    this.scanner.LoadSymbolOperator(this.valueBuffer);
                    this.valueType = IonType.Symbol;
                    break;
                case TextConstants.TokenSymbolQuoted:
                    clobCharsOnly = this.valueType == IonType.Clob;
                    c = this.scanner.LoadSingleQuotedString(this.valueBuffer, clobCharsOnly);
                    if (c == TextConstants.TokenEof)
                    {
                        throw new UnexpectedEofException();
                    }

                    this.valueType = IonType.Symbol;
                    break;
                case TextConstants.TokenStringDoubleQuote:
                    clobCharsOnly = this.valueType == IonType.Clob;
                    c = this.scanner.LoadDoubleQuotedString(this.valueBuffer, clobCharsOnly);
                    if (c == TextConstants.TokenEof)
                    {
                        throw new UnexpectedEofException();
                    }

                    this.valueType = IonType.String;
                    break;
                case TextConstants.TokenStringTripleQuote:
                    clobCharsOnly = this.valueType == IonType.Clob;
                    c = this.scanner.LoadTripleQuotedString(this.valueBuffer, clobCharsOnly);
                    if (c == TextConstants.TokenEof)
                    {
                        throw new UnexpectedEofException();
                    }

                    this.valueType = IonType.String;
                    break;
            }

            this.tokenContentLoaded = true;
        }

        private static int GetStateAtContainerStart(IonType container)
        {
            if (container == IonType.None)
            {
                return StateBeforeAnnotationDatagram;
            }

            Debug.Assert(container.IsContainer(), "container isContainer is false");

            switch (container)
            {
                default:
                    // should not happen
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
            {
                return StateBeforeAnnotationDatagram;
            }

            Debug.Assert(newContainer.IsContainer(), "newContainer IsContainer is false");

            switch (newContainer)
            {
                default:
                    // should not happen
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
            // (i.e. we're at the top level) then this isn't the state we should be in. We'll be in StateBeforeAnnotationDatagram
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

        private void ClearValue()
        {
            this.valueType = IonType.None;
            this.ClearValueBuffer();
            this.annotations.Clear();
            this.ClearFieldName();
            this.valueVariant.Clear();
            this.lobValuePosition = 0;
            this.lobBuffer = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearFieldName()
        {
            this.fieldName = null;
            this.fieldNameSid = SymbolToken.UnknownSid;
        }

        private void ParseNext()
        {
            var trailingWhitespace = false;

            var token = this.scanner.NextToken();
            while (true)
            {
                int action = TransitionActions[this.state, token];
                switch (action)
                {
                    default:
                        throw new InvalidTokenException(token);
                    case ActionNotDefined:
                        throw new IonException("Invalid state");
                    case ActionEof:
                        this.state = StateEof;
                        this.eof = true;
                        return;
                    case ActionLoadFieldName:
                        if (!this.IsInStruct)
                        {
                            throw new IonException("Field names have to be inside struct");
                        }

                        this.LoadTokenContents(token);
                        var symtok = this.ParseSymbolToken(this.valueBuffer, token);
                        this.SetFieldName(symtok);
                        this.ClearValueBuffer();
                        token = this.scanner.NextToken();
                        if (token != TextConstants.TokenColon)
                        {
                            throw new InvalidTokenException("Field name has to be followed by a colon");
                        }

                        this.scanner.MarkTokenFinished();
                        this.state = StateBeforeAnnotationContained;
                        token = this.scanner.NextToken();
                        break;
                    case ActionLoadAnnotation:
                        this.LoadTokenContents(token);
                        trailingWhitespace = this.scanner.SkipWhiteSpace();
                        if (!this.scanner.TrySkipDoubleColon())
                        {
                            this.state = GetStateAfterAnnotation(this.state, this.containerStack.Peek());
                            break;
                        }

                        var sym = this.ParseSymbolToken(this.valueBuffer, token);
                        this.annotations.Add(sym);
                        this.ClearValueBuffer();
                        this.scanner.MarkTokenFinished();

                        // Consumed the annotation, move on.
                        // note: that peekDoubleColon() consumed the two colons
                        // so nextToken won't see them
                        token = this.scanner.NextToken();
                        switch (token)
                        {
                            case TextConstants.TokenSymbolIdentifier:
                            case TextConstants.TokenSymbolQuoted:
                                // This may be another annotation, so stay in this state
                                // and come around the horn again to check it out.
                                break;
                            default:
                                // we leave the error handling to the transition
                                this.state = GetStateAfterAnnotation(this.state, this.containerStack.Peek());
                                break;
                        }

                        break;
                    case ActionStartStruct:
                        this.valueType = IonType.Struct;
                        this.expectedContainerClosingSymbol.Push(TextConstants.TokenCloseBrace);
                        this.state = StateBeforeFieldName;
                        return;
                    case ActionStartList:
                        this.valueType = IonType.List;
                        this.expectedContainerClosingSymbol.Push(TextConstants.TokenCloseSquare);
                        this.state = StateBeforeAnnotationContained;
                        return;
                    case ActionStartSexp:
                        this.valueType = IonType.Sexp;
                        this.expectedContainerClosingSymbol.Push(TextConstants.TokenCloseParen);
                        this.state = StateBeforeAnnotationSexp;
                        return;
                    case ActionStartLob:
                        switch (this.scanner.PeekLobStartPunctuation())
                        {
                            case TextConstants.TokenStringDoubleQuote:
                                this.lobToken = TextConstants.TokenStringDoubleQuote;
                                this.valueType = IonType.Clob;
                                break;
                            case TextConstants.TokenStringTripleQuote:
                                this.lobToken = TextConstants.TokenStringTripleQuote;
                                this.valueType = IonType.Clob;
                                break;
                            default:
                                this.state = StateInBlobContent;
                                this.lobToken = TextConstants.TokenOpenDoubleBrace;
                                this.valueType = IonType.Blob;
                                break;
                        }

                        return;
                    case ActionLoadScalar:
                        if (token == TextConstants.TokenSymbolIdentifier)
                        {
                            this.LoadTokenContents(token);

                            // token has been completely loaded
                            this.scanner.MarkTokenFinished();

                            this.valueKeyword = TextConstants.GetKeyword(this.valueBuffer, 0, this.valueBuffer.Length);
                            switch (this.valueKeyword)
                            {
                                default:
                                    this.valueType = IonType.Symbol;
                                    break;
                                case TextConstants.KeywordNull:
                                    this.ReadNullType(trailingWhitespace);
                                    break;
                                case TextConstants.KeywordTrue:
                                    this.valueType = IonType.Bool;
                                    this.valueVariant.BoolValue = true;
                                    break;
                                case TextConstants.KeywordFalse:
                                    this.valueType = IonType.Bool;
                                    this.valueVariant.BoolValue = false;
                                    break;
                                case TextConstants.KeywordNan:
                                    this.valueType = IonType.Float;
                                    this.ClearValueBuffer();
                                    this.valueVariant.DoubleValue = double.NaN;
                                    break;
                                case TextConstants.KeywordSid:
                                    var sid = TextConstants.DecodeSid(this.valueBuffer);
                                    this.valueVariant.IntValue = sid;
                                    this.valueType = IonType.Symbol;
                                    break;
                            }

                            // do not clear the buffer yet because LoadTokenContents() might be called again
                        }
                        else if (token == TextConstants.TokenDot)
                        {
                            this.valueType = IonType.Symbol;
                            this.ClearValueBuffer();
                            this.valueVariant.StringValue = ".";
                        }
                        else
                        {
                            // if it's not a symbol we just look at the token type
                            this.valueType = TextConstants.GetIonTypeOfToken(token);
                        }

                        this.state = GetStateAfterValue(this.containerStack.Peek());
                        return;
                    case ActionEatComma:
                        if (this.containerProhibitsCommas)
                        {
                            throw new InvalidTokenException(',');
                        }

                        this.state = this.containerIsStruct ? StateBeforeFieldName : StateBeforeAnnotationContained;
                        this.scanner.MarkTokenFinished();
                        token = this.scanner.NextToken();
                        break;
                    case ActionFinishDatagram:
                        Debug.Assert(this.CurrentDepth == 0, "CurrentDepth is not 0");
                        this.eof = true;
                        this.state = StateEof;
                        return;
                    case ActionFinishContainer:
                        this.ValidateClosingSymbol(token);
                        this.state = GetStateAfterContainer(this.containerStack.Peek());
                        this.eof = true;
                        return;
                    case ActionFinishLob:
                        this.state = GetStateAfterValue(this.containerStack.Peek());
                        return;
                    case ActionPlusInf:
                        this.valueType = IonType.Float;
                        this.ClearValueBuffer();
                        this.valueVariant.DoubleValue = double.PositiveInfinity;
                        this.state = GetStateAfterValue(this.containerStack.Peek());
                        return;
                    case ActionMinusInf:
                        this.valueType = IonType.Float;
                        this.ClearValueBuffer();
                        this.valueVariant.DoubleValue = double.NegativeInfinity;
                        this.state = GetStateAfterValue(this.containerStack.Peek());
                        return;
                }
            }
        }

        private void ValidateClosingSymbol(int token)
        {
            if (this.expectedContainerClosingSymbol.Count == 0)
            {
                throw new FormatException($"Unexpected {this.GetCharacterValueOfClosingContainerToken(token)}");
            }

            var latestContainerSymbol = this.expectedContainerClosingSymbol.Pop();
            if (latestContainerSymbol != token)
            {
                var currentToken = this.GetCharacterValueOfClosingContainerToken(token);
                var expectedToken = this.GetCharacterValueOfClosingContainerToken(latestContainerSymbol);

                throw new FormatException($"Illegal character: expected '{expectedToken}' character but encountered '{currentToken}'");
            }
        }

        private char GetCharacterValueOfClosingContainerToken(int tokenCode)
        {
            switch (tokenCode)
            {
                case TextConstants.TokenCloseParen: return ')';
                case TextConstants.TokenCloseBrace: return '}';
                case TextConstants.TokenCloseSquare: return ']';
                default: return ' ';
            }
        }

        private void ReadNullType(bool trailingWhitespace)
        {
            var kwt = trailingWhitespace ? TextConstants.KeywordNone : this.scanner.PeekNullTypeSymbol();
            switch (kwt)
            {
                case TextConstants.KeywordNull:
                    this.valueType = IonType.Null;
                    break;
                case TextConstants.KeywordBool:
                    this.valueType = IonType.Bool;
                    break;
                case TextConstants.KeywordInt:
                    this.valueType = IonType.Int;
                    break;
                case TextConstants.KeywordFloat:
                    this.valueType = IonType.Float;
                    break;
                case TextConstants.KeywordDecimal:
                    this.valueType = IonType.Decimal;
                    break;
                case TextConstants.KeywordTimestamp:
                    this.valueType = IonType.Timestamp;
                    break;
                case TextConstants.KeywordSymbol:
                    this.valueType = IonType.Symbol;
                    break;
                case TextConstants.KeywordString:
                    this.valueType = IonType.String;
                    break;
                case TextConstants.KeywordBlob:
                    this.valueType = IonType.Blob;
                    break;
                case TextConstants.KeywordClob:
                    this.valueType = IonType.Clob;
                    break;
                case TextConstants.KeywordList:
                    this.valueType = IonType.List;
                    break;
                case TextConstants.KeywordSexp:
                    this.valueType = IonType.Sexp;
                    break;
                case TextConstants.KeywordStruct:
                    this.valueType = IonType.Struct;
                    break;
                case TextConstants.KeywordNone:
                    this.valueType = IonType.Null;
                    break; // this happens when there isn't a '.' otherwise peek

                // throws the error or returns none
                default:
                    throw new IonException($"invalid keyword id ({kwt}) encountered while parsing a null");
            }

            // at this point we've consumed a dot '.' and it's preceding
            // whitespace
            // clear_value();
            this.valueVariant.SetNull(this.valueType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetFieldName(in SymbolToken symtok)
        {
            this.fieldName = symtok.Text;
            this.fieldNameSid = symtok.Sid;
        }

        private SymbolToken ParseSymbolToken(StringBuilder sb, int token)
        {
            if (token != TextConstants.TokenSymbolIdentifier)
            {
                return new SymbolToken(sb.ToString(), SymbolToken.UnknownSid);
            }

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
                    text = this.GetSymbolTable().FindKnownSymbol(sid);
                    break;
                default:
                    text = sb.ToString();
                    sid = this.GetSymbolTable().FindSymbolId(text);
                    break;
            }

            if (text == null && sid == 0)
            {
                return new SymbolToken(text, sid);
            }

            return new SymbolToken(text, sid, new ImportLocation(this.GetSymbolTable().Name, sid));
        }

        private void FinishValue()
        {
            if (this.scanner.UnfinishedToken)
            {
                this.scanner.FinishToken();
                this.state = GetStateAfterValue(this.containerStack.Peek());
            }

            this.hasNextCalled = false;
        }

        private class ContainerStack
        {
            private readonly RawTextReader rawTextReader;
            private IonType[] array;

            public ContainerStack(RawTextReader rawTextReader, int initialCapacity)
            {
                Debug.Assert(initialCapacity > 0, "initialCapacity is not greater than 0");
                this.rawTextReader = rawTextReader;
                this.array = new IonType[initialCapacity];
            }

            public int Count { get; private set; }

            public void PushContainer(IonType containerType)
            {
                this.EnsureCapacity(this.Count);
                this.array[this.Count] = containerType;
                this.SetContainerFlags(containerType);

                this.Count++;
            }

            public IonType Peek()
            {
                if (this.Count == 0)
                {
                    throw new IndexOutOfRangeException();
                }

                return this.array[this.Count - 1];
            }

            public void Pop()
            {
                if (this.Count == 0)
                {
                    throw new IndexOutOfRangeException();
                }

                this.Count--;

                this.rawTextReader.eof = false;
                this.rawTextReader.hasNextCalled = false;
                var topState = this.array[this.Count - 1];
                this.SetContainerFlags(topState);
                this.rawTextReader.state = GetStateAfterContainer(topState);
            }

            public IonType First() => this.array[0];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetContainerFlags(IonType type)
            {
                switch (type)
                {
                    default:
                        throw new IonException($"{type} is no container");
                    case IonType.Struct:
                        this.rawTextReader.containerIsStruct = true;
                        this.rawTextReader.containerProhibitsCommas = false;
                        return;
                    case IonType.List:
                        this.rawTextReader.containerIsStruct = false;
                        this.rawTextReader.containerProhibitsCommas = false;
                        return;
                    case IonType.Datagram:
                        this.rawTextReader.containerIsStruct = false;
                        this.rawTextReader.containerProhibitsCommas = true;
                        return;
                    case IonType.Sexp:
                        this.rawTextReader.containerIsStruct = false;
                        this.rawTextReader.containerProhibitsCommas = false;
                        return;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void EnsureCapacity(int forIndex)
            {
                if (forIndex < this.array.Length)
                {
                    return;
                }

                // resize
                Array.Resize(ref this.array, this.array.Length * 2);
            }
        }
    }
}
