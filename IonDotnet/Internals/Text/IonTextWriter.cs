using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using IonDotnet.Systems;
using IonDotnet.Utils;

namespace IonDotnet.Internals.Text
{
    /// <inheritdoc />
    /// <summary>
    /// Deals with writing Ion text form
    /// </summary>
    internal class IonTextWriter : IonSystemWriter
    {
        private enum SymbolVariant
        {
            Unknown,
            Identifier,
            Quoted,
            Operator
        }

        private readonly Stack<(IonType containerType, bool pendingComma)> _containerStack = new Stack<(IonType containerType, bool pendingComma)>(6);
        private readonly IonTextRawWriter _textWriter;
        private readonly IonTextOptions _options;

        private bool _isInStruct;
        private bool _pendingSeparator;
        private bool _isWritingIvm;
        private bool _followingLongString;
        private char _separatorCharacter;

        public IonTextWriter(TextWriter textWriter) : base(IonWriterBuilderBase.InitialIvmHandlingOption.Default)
        {
            _textWriter = new IonTextRawWriter(textWriter);
            _options = IonTextOptions.Default;
            _separatorCharacter = _options.PrettyPrint ? '\n' : ' ';
        }

        public IonTextWriter(TextWriter textWriter, IonTextOptions textOptions)
            : base(textOptions.WriteVersionMarker
                ? IonWriterBuilderBase.InitialIvmHandlingOption.Ensure
                : IonWriterBuilderBase.InitialIvmHandlingOption.Suppress)
        {
            _textWriter = new IonTextRawWriter(textWriter);
            _options = textOptions;
            _separatorCharacter = _options.PrettyPrint ? '\n' : ' ';
        }

        private void WriteLeadingWhiteSpace()
        {
            for (var ii = 0; ii < _containerStack.Count; ii++)
            {
                _textWriter.Write(' ');
                _textWriter.Write(' ');
            }
        }

        private void WriteSidLiteral(int sid)
        {
            if (_options.SymbolAsString)
            {
                _textWriter.Write('"');
            }

            _textWriter.Write('$');
            _textWriter.Write(sid);
            if (_options.SymbolAsString)
            {
                _textWriter.Write('"');
            }
        }

        private void WriteSymbolText(string text, SymbolVariant symbolVariant = SymbolVariant.Unknown)
        {
            if (_options.SymbolAsString)
            {
                if (_options.Json)
                {
                    _textWriter.WriteJsonString(text);
                }
                else
                {
                    _textWriter.WriteString(text);
                }

                return;
            }

            //TODO handle different kinds of SymbolVariant
            if (symbolVariant == SymbolVariant.Unknown)
            {
                symbolVariant = GetSymbolVariant(text);
            }

            switch (symbolVariant)
            {
                case SymbolVariant.Identifier:
                    _textWriter.Write(text);
                    break;
                case SymbolVariant.Operator:
                    if (_containerStack.Count > 0 && _containerStack.Peek().containerType == IonType.Sexp)
                    {
                        _textWriter.Write(text);
                        break;
                    }

                    goto case SymbolVariant.Quoted;
                case SymbolVariant.Quoted:
                    _textWriter.WriteSingleQuotedSymbol(text);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(symbolVariant), symbolVariant, null);
            }
        }

        private void WriteFieldNameToken(SymbolToken token)
        {
            if (token.Text == null)
            {
                //unknown text, write id
                WriteSidLiteral(token.Sid);
                return;
            }

            WriteSymbolText(token.Text);
        }

        private void WriteAnnotations()
        {
            foreach (var annotation in _annotations)
            {
                WriteAnnotationToken(annotation);
                _textWriter.Write("::");
            }
        }

        private void WriteAnnotationToken(SymbolToken annotation)
        {
            if (annotation.Text == null)
            {
                //unknown text, write sid
                _textWriter.Write('$');
                _textWriter.Write(annotation.Sid);
                return;
            }

            _textWriter.WriteSymbol(annotation.Text);
        }

        /// <summary>
        /// Write a separator between values
        /// </summary>
        /// <param name="followingLongString">If this is a separator to separate long string</param>
        /// <returns>If long string separation continues</returns>
        private bool WriteSeparator(bool followingLongString)
        {
            if (_options.PrettyPrint)
            {
                if (_pendingSeparator && _separatorCharacter > ' ')
                {
                    // Only bother if the separator is non-whitespace.
                    _textWriter.Write(_separatorCharacter);
                    followingLongString = false;
                }

                if (_ivmHandlingOption == IonWriterBuilderBase.InitialIvmHandlingOption.Default)
                {
                    //this mean we've written something
                    _textWriter.Write(_options.LineSeparator);
                }

                WriteLeadingWhiteSpace();
            }
            else if (_pendingSeparator)
            {
                _textWriter.Write(_separatorCharacter);
                if (_separatorCharacter > ' ')
                {
                    followingLongString = false;
                }
            }

            return followingLongString;
        }

        protected override void WriteSymbolAsIs(SymbolToken symbolToken)
        {
            if (symbolToken == default)
            {
                WriteNull(IonType.Symbol);
                return;
            }

            StartValue();
            if (symbolToken.Text is null)
            {
                WriteSidLiteral(symbolToken.Sid);
            }
            else
            {
                //we write all symbol values with single-quote
                WriteSymbolText(symbolToken.Text, SymbolVariant.Quoted);
            }

            CloseValue();
        }

        protected override void WriteIonVersionMarker(ISymbolTable systemSymtab)
        {
            _isWritingIvm = true;

            StartValue();
            WriteSymbolText(systemSymtab.IonVersionId, SymbolVariant.Identifier);
            CloseValue();

            _isWritingIvm = false;
        }

        protected override void StartValue()
        {
            base.StartValue();
            var followingLongString = WriteSeparator(_followingLongString);

            //write field name
            if (_isInStruct)
            {
                var sym = AssumeFieldNameSymbol();
                WriteFieldNameToken(sym);
                _textWriter.Write(':');
                if (_options.PrettyPrint)
                {
                    _textWriter.Write(' ');
                }

                ClearFieldName();
                followingLongString = false;
            }

            // write annotations only if they exist and we're not currently writing an IVM
            if (_annotations.Count > 0 && !_isWritingIvm)
            {
                if (!_options.SkipAnnotations)
                {
                    WriteAnnotations();
                    followingLongString = false;
                }

                _annotations.Clear();
            }

            _followingLongString = followingLongString;
        }

        private void CloseValue()
        {
            EndValue();
            _pendingSeparator = true;
            _followingLongString = false;

            // TODO Flush if a top-level-value was written
            if (GetDepth() == 0)
            {
//                Flush();
            }
        }

//        public override Task FlushAsync() => _textWriter.FlushAsync();
        public override void Flush() => _textWriter.Flush();

        public override void WriteNull()
        {
            StartValue();
            _textWriter.Write("null");
            CloseValue();
        }

        public override void WriteNull(IonType type)
        {
            StartValue();
            string nullImage;
            if (_options.UntypedNull)
            {
                nullImage = "null";
            }
            else
            {
                switch (type)
                {
                    case IonType.Null:
                        nullImage = "null";
                        break;
                    case IonType.Bool:
                        nullImage = "null.bool";
                        break;
                    case IonType.Int:
                        nullImage = "null.int";
                        break;
                    case IonType.Float:
                        nullImage = "null.float";
                        break;
                    case IonType.Decimal:
                        nullImage = "null.decimal";
                        break;
                    case IonType.Timestamp:
                        nullImage = "null.timestamp";
                        break;
                    case IonType.Symbol:
                        nullImage = "null.symbol";
                        break;
                    case IonType.String:
                        nullImage = "null.string";
                        break;
                    case IonType.Clob:
                        nullImage = "null.clob";
                        break;
                    case IonType.Blob:
                        nullImage = "null.blob";
                        break;
                    case IonType.List:
                        nullImage = "null.list";
                        break;
                    case IonType.Sexp:
                        nullImage = "null.sexp";
                        break;
                    case IonType.Struct:
                        nullImage = "null.struct";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(type), type, null);
                }
            }

            _textWriter.Write(nullImage);
            CloseValue();
        }

        public override void WriteBool(bool value)
        {
            StartValue();
            _textWriter.Write(value ? "true" : "false");
            CloseValue();
        }

        public override void WriteInt(long value)
        {
            StartValue();
            _textWriter.Write(value);
            CloseValue();
        }

        public override void WriteInt(BigInteger value)
        {
            StartValue();
            //this is not optimal but it's not a common use case
            _textWriter.Write(value.ToString());
            EndValue();
        }

        public override void WriteFloat(double value)
        {
            StartValue();
            _textWriter.Write(value);
            CloseValue();
        }

        public override void WriteDecimal(decimal value) => WriteDecimal(new BigDecimal(value));

        public override void WriteDecimal(BigDecimal value)
        {
            StartValue();
            _textWriter.Write(value);
            CloseValue();
        }

        public override void WriteTimestamp(Timestamp value)
        {
            StartValue();

            if (_options.TimestampAsMillis)
            {
                _textWriter.Write(value.Milliseconds);
            }
            else
            {
                _textWriter.Write(value.ToString());
            }

            CloseValue();
        }

        public override void WriteString(string value)
        {
            StartValue();
            if (value != null && !_followingLongString && _options.LongStringThreshold < value.Length)
            {
                _textWriter.WriteLongString(value);
                CloseValue();
                //CloseValue This sets _following_long_string = false so we must overwrite
                _followingLongString = true;
                return;
            }

            //double-quoted
            if (_options.Json)
            {
                _textWriter.WriteJsonString(value);
            }
            else
            {
                _textWriter.WriteString(value);
            }

            CloseValue();
        }

        public override void WriteBlob(ReadOnlySpan<byte> value)
        {
            StartValue();

            //TODO high-perf no-alloc encoding?
#if NET45 || NETSTANDARD1_3 || NETSTANDARD2_0
            var base64 = Convert.ToBase64String(value.ToArray());
#else
            var base64 = Convert.ToBase64String(value);
#endif

            //TODO blob as string?
            _textWriter.Write("{{");
            if (_options.PrettyPrint)
            {
                _textWriter.Write(' ');
            }

            _textWriter.Write(base64);
            if (_options.PrettyPrint)
            {
                _textWriter.Write(' ');
            }

            _textWriter.Write("}}");

            CloseValue();
        }

        public override void WriteClob(ReadOnlySpan<byte> value)
        {
            StartValue();

            _textWriter.Write("{{");
            if (_options.PrettyPrint)
            {
                _textWriter.Write(' ');
            }

            _textWriter.WriteClobAsString(value);
            if (_options.PrettyPrint)
            {
                _textWriter.Write(' ');
            }

            _textWriter.Write("}}");

            CloseValue();
        }

        public override void Dispose()
        {
            //TODO is there anything to do here?
        }

        public override void Finish()
        {
//            _textWriter.Flush();
        }

//        public override Task FinishAsync()
//        {
//            throw new NotImplementedException();
//        }

        public override void StepIn(IonType type)
        {
            StartValue();
            char opener;
            switch (type)
            {
                case IonType.List:
                    _isInStruct = false;
                    opener = '[';
                    break;
                case IonType.Sexp:
                    //TODO handle sexp as list option
                    opener = '(';
                    _isInStruct = false;
                    break;
                case IonType.Struct:
                    opener = '{';
                    _isInStruct = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            _containerStack.Push((type, _pendingSeparator));
            //determine the separator in this container
            switch (type)
            {
                case IonType.Struct:
                case IonType.List:
                    _separatorCharacter = ',';
                    break;
                case IonType.Sexp:
                    _separatorCharacter = ' ';
                    break;
                default:
                    _separatorCharacter = _options.PrettyPrint ? '\n' : ' ';
                    break;
            }

            _textWriter.Write(opener);
            //we've started the value and written something, ivm no longer needed
            _ivmHandlingOption = IonWriterBuilderBase.InitialIvmHandlingOption.Default;
            _pendingSeparator = false;
            _followingLongString = false;
        }

        public override void StepOut()
        {
            if (_containerStack.Count == 0)
                throw new InvalidOperationException("Already at top-level");

            var top = _containerStack.Pop();

            var parentType = _containerStack.Count == 0 ? IonType.Datagram : _containerStack.Peek().containerType;
            switch (parentType)
            {
                case IonType.Sexp:
                    _isInStruct = false;
                    _separatorCharacter = ' ';
                    break;
                case IonType.List:
                    _isInStruct = false;
                    _separatorCharacter = ',';
                    break;
                case IonType.Struct:
                    _isInStruct = true;
                    _separatorCharacter = ',';
                    break;
                default:
                    _isInStruct = false;
                    _separatorCharacter = _options.PrettyPrint ? '\n' : ' ';
                    break;
            }

            _pendingSeparator = top.pendingComma;
            char closer;
            switch (top.containerType)
            {
                default:
                    //shoud not happen
                    throw new IonException($"{top.containerType} is no container");
                case IonType.List:
                    closer = ']';
                    break;
                case IonType.Sexp:
                    closer = ')';
                    break;
                case IonType.Struct:
                    closer = '}';
                    break;
            }

            //close the collection
            if (_options.PrettyPrint)
            {
                _textWriter.Write(_options.LineSeparator);
                WriteLeadingWhiteSpace();
            }

            _textWriter.Write(closer);
            CloseValue();
        }

        public override bool IsInStruct => _isInStruct;

        public override int GetDepth() => _containerStack.Count;

        private static bool IsIdentifierPart(char c)
        {
            if (c >= 'a' && c <= 'z')
                return true;
            if (c >= 'A' && c <= 'Z')
                return true;
            if (c >= '0' && c <= '9')
                return true;

            return c == '_' || c == '$';
        }

        private static bool IsIdentifierKeyword(string text)
        {
            var pos = 0;
            var valuelen = text.Length;

            if (valuelen == 0)
                return false;

            var keyword = false;

            // there has to be at least 1 character or we wouldn't be here
            switch (text[pos++])
            {
                case '$':
                    if (valuelen == 1)
                        return false;
                    while (pos < valuelen)
                    {
                        var c = text[pos++];
                        if (!char.IsDigit(c))
                            return false;
                    }

                    return true;
                case 'f':
                    if (valuelen == 5 //      'f'
                        && text[pos++] == 'a'
                        && text[pos++] == 'l'
                        && text[pos++] == 's'
                        && text[pos] == 'e'
                    )
                    {
                        keyword = true;
                    }

                    break;
                case 'n':
                    if (valuelen == 4 //      'n'
                        && text[pos++] == 'u'
                        && text[pos++] == 'l'
                        && text[pos++] == 'l'
                    )
                    {
                        keyword = true;
                    }
                    else if (valuelen == 3 // 'n'
                             && text[pos++] == 'a'
                             && text[pos] == 'n'
                    )
                    {
                        keyword = true;
                    }

                    break;
                case 't':
                    if (valuelen == 4 //      't'
                        && text[pos++] == 'r'
                        && text[pos++] == 'u'
                        && text[pos] == 'e'
                    )
                    {
                        keyword = true;
                    }

                    break;
            }

            return keyword;
        }

        private static bool IsOperatorPart(char c)
        {
            //TODO stackalloc
            if (!Characters.Is8BitChar(c))
                return false;
            Span<int> operatorChars = stackalloc int[]
            {
                '<', '>', '=', '+', '-', '*', '&', '^', '%',
                '~', '/', '?', '.', ';', '!', '|', '@', '`', '#'
            };

            foreach (var operatorChar in operatorChars)
            {
                if (operatorChar == c)
                    return true;
            }

            return false;
        }

        private static SymbolVariant GetSymbolVariant(string symbol)
        {
            var length = symbol.Length; // acts as null check

            // If the symbol's text matches an Ion keyword or it's an empty symbol, we must quote it.
            // Eg, the symbol 'false' and '' must be rendered quoted.
            if (length == 0 || IsIdentifierKeyword(symbol))
            {
                return SymbolVariant.Quoted;
            }

            var c = symbol[0];
            // Surrogates are neither identifierStart nor operatorPart, so the
            // first one we hit will fall through and return QUOTED.
            // TODO test that

            if (IsIdentifierPart(c))
            {
                for (var ii = 0; ii < length; ii++)
                {
                    c = symbol[ii];
                    if (c == '\'' || c < 32 || c > 126 || !IsIdentifierPart(c))
                        return SymbolVariant.Quoted;
                }

                return SymbolVariant.Identifier;
            }

            if (!IsOperatorPart(c))
                return SymbolVariant.Quoted;

            for (var ii = 0; ii < length; ii++)
            {
                c = symbol[ii];
                // We don't need to look for escapes since all
                // operator characters are ASCII.
                if (!IsOperatorPart(c))
                    return SymbolVariant.Quoted;
            }

            return SymbolVariant.Operator;
        }
    }
}
