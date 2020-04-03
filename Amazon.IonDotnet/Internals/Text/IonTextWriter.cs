﻿/*
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Amazon.IonDotnet.Builders;
using Amazon.IonDotnet.Utils;

namespace Amazon.IonDotnet.Internals.Text
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
        private bool _valueStarted;
        private char _separatorCharacter;

        public IonTextWriter(TextWriter textWriter, IEnumerable<ISymbolTable> imports = null) : this(textWriter, IonTextOptions.Default, imports)
        {
        }

        public IonTextWriter(TextWriter textWriter, IonTextOptions textOptions, IEnumerable<ISymbolTable> imports = null)
        {
            _textWriter = new IonTextRawWriter(textWriter);
            _options = textOptions;
            InitializeImportList(imports);
            _separatorCharacter = _options.PrettyPrint ? '\n' : ' ';
        }


        private void InitializeImportList(IEnumerable<ISymbolTable> imports)
        {
            if (imports == null)
            {
                return;
            }

            foreach (var table in imports)
            {
                if (!table.IsShared)
                {
                    throw new ArgumentException($"Import table is not shared: {table}");
                }

                if (table.IsSystem)
                {
                    continue;
                }

                symbolTable.Imports.Add(table);
            }

            symbolTable.Refresh();
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

        private void WriteSymbolText(string text)
        {
            if (_options.SymbolAsString)
            {
                _textWriter.WriteString(text);
                return;
            }

            // Determine the variant for writing.
            SymbolVariant symbolVariant = GetSymbolVariant(text);

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
            foreach (var annotation in annotations)
            {
                WriteAnnotationToken(annotation);
                _textWriter.Write("::");
            }
        }

        private void WriteAnnotationToken(SymbolToken annotation)
        {
            var text = annotation.Text;
            if (text is null)
            {
                //unknown text, write sid
                _textWriter.Write('$');
                _textWriter.Write(annotation.Sid < 0 ? 0 : annotation.Sid);
                return;
            }

            var needQuote = GetSymbolVariant(text) != SymbolVariant.Identifier;
            //TODO extra need quote
            if (needQuote)
            {
                _textWriter.WriteSingleQuotedSymbol(text);
                return;
            }

            _textWriter.WriteSymbol(text);
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

                if (_valueStarted)
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
                WriteSymbolText(symbolToken.Text);
            }

            CloseValue();
        }

        protected override void WriteIonVersionMarker(ISymbolTable systemSymtab)
        {
            _isWritingIvm = true;

            StartValue();
            WriteSymbolText(systemSymtab.IonVersionId);
            CloseValue();

            _isWritingIvm = false;
        }

        protected void StartValue()
        {
            if (!_valueStarted)
            {
                _valueStarted = true;
                if (_options.WriteVersionMarker)
                {
                    WriteIonVersionMarker();
                }

                //write the imports here
                WriteImports();
            }

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
            if (annotations.Count > 0 && !_isWritingIvm)
            {
                if (!_options.SkipAnnotations)
                {
                    WriteAnnotations();
                    followingLongString = false;
                }

                annotations.Clear();
            }

            _followingLongString = followingLongString;
        }

        /// <summary>
        /// Write all the imported symbol tables
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void WriteImports()
        {
            //only write local symtab if we import sth more than just system table
            if (symbolTable.Imports.Count <= 1)
            {
                return;
            }

            AddTypeAnnotation(SystemSymbols.IonSymbolTable);
            StepIn(IonType.Struct);
            SetFieldName(SystemSymbols.Imports);
            StepIn(IonType.List);

            foreach (var importedTable in symbolTable.Imports)
            {
                if (importedTable.IsSystem)
                {
                    continue;
                }

                this.WriteImportTable(importedTable);
            }

            StepOut();
            StepOut();
        }

        private void CloseValue()
        {
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

        /// <summary>
        /// Override this so that the imports are written first.
        /// </summary>
        /// <param name="annotation"></param>
        public override void AddTypeAnnotationSymbol(SymbolToken annotation)
        {
            if (!_valueStarted)
            {
                _valueStarted = true;
                if (_options.WriteVersionMarker)
                {
                    WriteIonVersionMarker();
                }

                //write the imports here
                WriteImports();
            }

            base.AddTypeAnnotationSymbol(annotation);
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
            CloseValue();
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
            _textWriter.WriteString(value);

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

        /// <summary>
        /// Returns true if c is part of an symbol identifier string.
        /// </summary>
        /// <param name="c">Character</param>
        /// <param name="start">True if character is the start of the text</param>
        private static bool IsIdentifierPart(char c, bool start)
        {
            if (c >= 'a' && c <= 'z')
                return true;
            if (c >= 'A' && c <= 'Z')
                return true;
            if (c >= '0' && c <= '9')
                return !start;

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

            if (IsIdentifierPart(c, true))
            {
                for (var ii = 0; ii < length; ii++)
                {
                    c = symbol[ii];
                    if (c == '\'' || c < 32 || c > 126 || !IsIdentifierPart(c, false))
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
