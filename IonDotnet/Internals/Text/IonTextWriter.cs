using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using IonDotnet.Systems;

namespace IonDotnet.Internals.Text
{
    internal class IonTextWriter : SystemWriter
    {
        private readonly Stack<(IonType containerType, bool pendingComma)> _containerStack = new Stack<(IonType containerType, bool pendingComma)>(6);
        private readonly IonTextRawWriter _textWriter;
        private readonly IonTextOptions _options;

        private bool _isInStruct;
        private bool _pendingSeparator;
        private bool _isWritingIvm;
        private bool _followingLongString;
        private char _separatorCharacter;

        public IonTextWriter(TextWriter textWriter, IonTextOptions textOptions,
            IonWriterBuilderBase.InitialIvmHandlingOption ivmHandlingOption = IonWriterBuilderBase.InitialIvmHandlingOption.Default)
            : base(ivmHandlingOption)
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

        private void WriteSymbolToken(string value)
        {
            if (_options.SymbolAsString)
            {
                if (_options.StringAsJson)
                {
                    _textWriter.WriteJsonString(value);
                }
                else
                {
                    _textWriter.WriteString(value);
                }

                return;
            }

            //TODO handle different kinds of SymbolVariant
            _textWriter.Write(value);
        }

        private void WriteFieldNameToken(SymbolToken token)
        {
            if (token.Text == null)
            {
                //unknown text, write id
                WriteSidLiteral(token.Sid);
                return;
            }

            WriteSymbolToken(token.Text);
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

                _textWriter.Write(_options.LineSeparator);
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

            // Flush if a top-level-value was written
            if (GetDepth() == 0)
            {
                Flush();
            }
        }

        public override void Flush()
        {
            _textWriter.Flush();
        }

        public override void WriteNull()
        {
            StartValue();
            _textWriter.Write("null");
            CloseValue();
        }

        public override void WriteNull(IonType type)
        {
            StartValue();
            string nullimage;
            if (_options.UntypedNull)
            {
                nullimage = "null";
            }
            else
            {
                switch (type)
                {
                    case IonType.Null:
                        nullimage = "null";
                        break;
                    case IonType.Bool:
                        nullimage = "null.bool";
                        break;
                    case IonType.Int:
                        nullimage = "null.int";
                        break;
                    case IonType.Float:
                        nullimage = "null.float";
                        break;
                    case IonType.Decimal:
                        nullimage = "null.decimal";
                        break;
                    case IonType.Timestamp:
                        nullimage = "null.timestamp";
                        break;
                    case IonType.Symbol:
                        nullimage = "null.symbol";
                        break;
                    case IonType.String:
                        nullimage = "null.string";
                        break;
                    case IonType.Clob:
                        nullimage = "null.clob";
                        break;
                    case IonType.Blob:
                        nullimage = "null.blob";
                        break;
                    case IonType.List:
                        nullimage = "null.list";
                        break;
                    case IonType.Sexp:
                        nullimage = "null.sexp";
                        break;
                    case IonType.Struct:
                        nullimage = "null.struct";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(type), type, null);
                }
            }

            _textWriter.Write(nullimage);
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

        public override void WriteDecimal(decimal value)
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

        public override void WriteSymbol(string symbol)
        {
            throw new NotImplementedException();
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
            if (_options.StringAsJson)
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
            var base64 = Convert.ToBase64String(value);

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
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }

        public override void Flush(Stream outputStream)
        {
            throw new NotImplementedException();
        }

        public override void Flush(ref byte[] bytes)
        {
            throw new NotImplementedException();
        }

        public override int Flush(Memory<byte> buffer)
        {
            throw new NotImplementedException();
        }

        public override void Finish()
        {
            _textWriter.Flush();
        }

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
            _pendingSeparator = false;
            _followingLongString = false;
        }

        public override void StepOut()
        {
            if (_containerStack.Count == 0)
                throw new InvalidOperationException("Already at top-level");

            var top = _containerStack.Pop();
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
    }
}
