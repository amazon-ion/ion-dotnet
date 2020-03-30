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
    using System.IO;
    using System.Numerics;
    using Amazon.IonDotnet.Builders;
    using Amazon.IonDotnet.Utils;

    /// <inheritdoc />
    /// <summary>
    /// Deals with writing Ion text form.
    /// </summary>
    internal class IonTextWriter : IonSystemWriter
    {
        private readonly Stack<(IonType containerType, bool pendingComma)> containerStack = new Stack<(IonType containerType, bool pendingComma)>(6);
        private readonly IonTextRawWriter textWriter;
        private readonly IonTextOptions options;

        private bool isInStruct;
        private bool pendingSeparator;
        private bool isWritingIvm;
        private bool followingLongString;
        private bool valueStarted;
        private char separatorCharacter;

        public IonTextWriter(TextWriter textWriter, IEnumerable<ISymbolTable> imports = null)
            : this(textWriter, IonTextOptions.Default, imports)
        {
        }

        public IonTextWriter(TextWriter textWriter, IonTextOptions textOptions, IEnumerable<ISymbolTable> imports = null)
        {
            this.textWriter = new IonTextRawWriter(textWriter);
            this.options = textOptions;
            this.InitializeImportList(imports);
            this.separatorCharacter = this.options.PrettyPrint ? '\n' : ' ';
        }

        private enum SymbolVariant
        {
            Unknown,
            Identifier,
            Quoted,
            Operator,
        }

        public override bool IsInStruct => this.isInStruct;

        public override void Flush() => this.textWriter.Flush();

        /// <summary>
        /// Override this so that the imports are written first.
        /// </summary>
        /// <param name="annotation">Annotation to add.</param>
        public override void AddTypeAnnotationSymbol(SymbolToken annotation)
        {
            if (!this.valueStarted)
            {
                this.valueStarted = true;
                if (this.options.WriteVersionMarker)
                {
                    this.WriteIonVersionMarker();
                }

                // write the imports here
                this.WriteImports();
            }

            base.AddTypeAnnotationSymbol(annotation);
        }

        public override void WriteNull()
        {
            this.StartValue();
            this.textWriter.Write("null");
            this.CloseValue();
        }

        public override void WriteNull(IonType type)
        {
            this.StartValue();
            string nullImage;
            if (this.options.UntypedNull)
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

            this.textWriter.Write(nullImage);
            this.CloseValue();
        }

        public override void WriteBool(bool value)
        {
            this.StartValue();
            this.textWriter.Write(value ? "true" : "false");
            this.CloseValue();
        }

        public override void WriteInt(long value)
        {
            this.StartValue();
            this.textWriter.Write(value);
            this.CloseValue();
        }

        public override void WriteInt(BigInteger value)
        {
            this.StartValue();
            this.textWriter.Write(value.ToString());
            this.CloseValue();
        }

        public override void WriteFloat(double value)
        {
            this.StartValue();
            this.textWriter.Write(value);
            this.CloseValue();
        }

        public override void WriteDecimal(decimal value)
        {
            this.StartValue();
            this.textWriter.Write(value);
            this.CloseValue();
        }

        public override void WriteDecimal(BigDecimal value)
        {
            this.StartValue();
            this.textWriter.Write(value);
            this.CloseValue();
        }

        public override void WriteTimestamp(Timestamp value)
        {
            this.StartValue();

            if (this.options.TimestampAsMillis)
            {
                this.textWriter.Write(value.Milliseconds);
            }
            else
            {
                this.textWriter.Write(value.ToString());
            }

            this.CloseValue();
        }

        public override void WriteString(string value)
        {
            this.StartValue();
            if (value != null && !this.followingLongString && this.options.LongStringThreshold < value.Length)
            {
                this.textWriter.WriteLongString(value);
                this.CloseValue();

                // CloseValue sets followingLongString = false so we must overwrite
                this.followingLongString = true;
                return;
            }

            // double-quoted
            this.textWriter.WriteString(value);

            this.CloseValue();
        }

        public override void WriteBlob(ReadOnlySpan<byte> value)
        {
            this.StartValue();

#if NET45 || NETSTANDARD2_0
            var base64 = Convert.ToBase64String(value.ToArray());
#else
            var base64 = Convert.ToBase64String(value);
#endif

            this.textWriter.Write("{{");
            if (this.options.PrettyPrint)
            {
                this.textWriter.Write(' ');
            }

            this.textWriter.Write(base64);
            if (this.options.PrettyPrint)
            {
                this.textWriter.Write(' ');
            }

            this.textWriter.Write("}}");

            this.CloseValue();
        }

        public override void WriteClob(ReadOnlySpan<byte> value)
        {
            this.StartValue();

            this.textWriter.Write("{{");
            if (this.options.PrettyPrint)
            {
                this.textWriter.Write(' ');
            }

            this.textWriter.WriteClobAsString(value);
            if (this.options.PrettyPrint)
            {
                this.textWriter.Write(' ');
            }

            this.textWriter.Write("}}");

            this.CloseValue();
        }

        public override void Dispose()
        {
        }

        public override void Finish()
        {
        }

        public override void StepIn(IonType type)
        {
            this.StartValue();
            char opener;
            switch (type)
            {
                case IonType.List:
                    this.isInStruct = false;
                    opener = '[';
                    break;
                case IonType.Sexp:
                    // TODO: handle sexp as list option
                    opener = '(';
                    this.isInStruct = false;
                    break;
                case IonType.Struct:
                    opener = '{';
                    this.isInStruct = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            this.containerStack.Push((type, this.pendingSeparator));

            // determine the separator in this container
            switch (type)
            {
                case IonType.Struct:
                case IonType.List:
                    this.separatorCharacter = ',';
                    break;
                case IonType.Sexp:
                    this.separatorCharacter = ' ';
                    break;
                default:
                    this.separatorCharacter = this.options.PrettyPrint ? '\n' : ' ';
                    break;
            }

            this.textWriter.Write(opener);

            // we've started the value and written something, ivm no longer needed
            this.pendingSeparator = false;
            this.followingLongString = false;
        }

        public override void StepOut()
        {
            if (this.containerStack.Count == 0)
            {
                throw new InvalidOperationException("Already at top-level");
            }

            var (containerType, pendingComma) = this.containerStack.Pop();

            var parentType = this.containerStack.Count == 0 ? IonType.Datagram : this.containerStack.Peek().containerType;
            switch (parentType)
            {
                case IonType.Sexp:
                    this.isInStruct = false;
                    this.separatorCharacter = ' ';
                    break;
                case IonType.List:
                    this.isInStruct = false;
                    this.separatorCharacter = ',';
                    break;
                case IonType.Struct:
                    this.isInStruct = true;
                    this.separatorCharacter = ',';
                    break;
                default:
                    this.isInStruct = false;
                    this.separatorCharacter = this.options.PrettyPrint ? '\n' : ' ';
                    break;
            }

            this.pendingSeparator = pendingComma;
            char closer;
            switch (containerType)
            {
                default:
                    // shoud not happen
                    throw new IonException($"{containerType} is no container");
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

            // close the collection
            if (this.options.PrettyPrint)
            {
                this.textWriter.Write(this.options.LineSeparator);
                this.WriteLeadingWhiteSpace();
            }

            this.textWriter.Write(closer);
            this.CloseValue();
        }

        public override int GetDepth() => this.containerStack.Count;

        protected override void WriteSymbolAsIs(SymbolToken symbolToken)
        {
            if (symbolToken == default)
            {
                this.WriteNull(IonType.Symbol);
                return;
            }

            this.StartValue();
            if (symbolToken.Text is null)
            {
                this.WriteSidLiteral(symbolToken.Sid);
            }
            else
            {
                this.WriteSymbolText(symbolToken.Text);
            }

            this.CloseValue();
        }

        protected override void WriteIonVersionMarker(ISymbolTable systemSymtab)
        {
            this.isWritingIvm = true;

            this.StartValue();
            this.WriteSymbolText(systemSymtab.IonVersionId);
            this.CloseValue();

            this.isWritingIvm = false;
        }

        protected void StartValue()
        {
            if (!this.valueStarted)
            {
                this.valueStarted = true;
                if (this.options.WriteVersionMarker)
                {
                    this.WriteIonVersionMarker();
                }

                // write the imports here
                this.WriteImports();
            }

            var followingLongString = this.WriteSeparator(this.followingLongString);

            // write field name
            if (this.isInStruct)
            {
                var sym = this.AssumeFieldNameSymbol();
                this.WriteFieldNameToken(sym);
                this.textWriter.Write(':');
                if (this.options.PrettyPrint)
                {
                    this.textWriter.Write(' ');
                }

                this.ClearFieldName();
                followingLongString = false;
            }

            // write annotations only if they exist and we're not currently writing an IVM
            if (this.annotations.Count > 0 && !this.isWritingIvm)
            {
                if (!this.options.SkipAnnotations)
                {
                    this.WriteAnnotations();
                    followingLongString = false;
                }

                this.annotations.Clear();
            }

            this.followingLongString = followingLongString;
        }

        /// <summary>
        /// Returns true if c is part of an symbol identifier string.
        /// </summary>
        /// <param name="c">Character.</param>
        /// <param name="start">True if character is the start of the text.</param>
        private static bool IsIdentifierPart(char c, bool start)
        {
            if (c >= 'a' && c <= 'z')
            {
                return true;
            }

            if (c >= 'A' && c <= 'Z')
            {
                return true;
            }

            if (c >= '0' && c <= '9')
            {
                return !start;
            }

            return c == '_' || c == '$';
        }

        private static bool IsIdentifierKeyword(string text)
        {
            var pos = 0;
            var valuelen = text.Length;

            if (valuelen == 0)
            {
                return false;
            }

            var keyword = false;

            // there has to be at least 1 character or we wouldn't be here
            switch (text[pos++])
            {
                case '$':
                    if (valuelen == 1)
                    {
                        return false;
                    }

                    while (pos < valuelen)
                    {
                        var c = text[pos++];
                        if (!char.IsDigit(c))
                        {
                            return false;
                        }
                    }

                    return true;
                case 'f':
                    if (valuelen == 5 // 'f'
                        && text[pos++] == 'a'
                        && text[pos++] == 'l'
                        && text[pos++] == 's'
                        && text[pos] == 'e')
                    {
                        keyword = true;
                    }

                    break;
                case 'n':
                    if (valuelen == 4 // 'n'
                        && text[pos++] == 'u'
                        && text[pos++] == 'l'
                        && text[pos++] == 'l')
                    {
                        keyword = true;
                    }
                    else if (valuelen == 3 // 'n'
                             && text[pos++] == 'a'
                             && text[pos] == 'n')
                    {
                        keyword = true;
                    }

                    break;
                case 't':
                    if (valuelen == 4 // 't'
                        && text[pos++] == 'r'
                        && text[pos++] == 'u'
                        && text[pos] == 'e')
                    {
                        keyword = true;
                    }

                    break;
            }

            return keyword;
        }

        private static bool IsOperatorPart(char c)
        {
            if (!Characters.Is8BitChar(c))
            {
                return false;
            }

            Span<int> operatorChars = stackalloc int[]
            {
                '<',
                '>',
                '=',
                '+',
                '-',
                '*',
                '&',
                '^',
                '%',
                '~',
                '/',
                '?',
                '.',
                ';',
                '!',
                '|',
                '@',
                '`',
                '#',
            };

            foreach (var operatorChar in operatorChars)
            {
                if (operatorChar == c)
                {
                    return true;
                }
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
            if (IsIdentifierPart(c, true))
            {
                for (var ii = 0; ii < length; ii++)
                {
                    c = symbol[ii];
                    if (c == '\'' || c < 32 || c > 126 || !IsIdentifierPart(c, false))
                    {
                        return SymbolVariant.Quoted;
                    }
                }

                return SymbolVariant.Identifier;
            }

            if (!IsOperatorPart(c))
            {
                return SymbolVariant.Quoted;
            }

            for (var ii = 0; ii < length; ii++)
            {
                c = symbol[ii];

                // We don't need to look for escapes since all
                // operator characters are ASCII.
                if (!IsOperatorPart(c))
                {
                    return SymbolVariant.Quoted;
                }
            }

            return SymbolVariant.Operator;
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

                this.symbolTable.Imports.Add(table);
            }

            this.symbolTable.Refresh();
        }

        private void WriteLeadingWhiteSpace()
        {
            for (var ii = 0; ii < this.containerStack.Count; ii++)
            {
                this.textWriter.Write(' ');
                this.textWriter.Write(' ');
            }
        }

        private void WriteSidLiteral(int sid)
        {
            if (this.options.SymbolAsString)
            {
                this.textWriter.Write('"');
            }

            this.textWriter.Write('$');
            this.textWriter.Write(sid);
            if (this.options.SymbolAsString)
            {
                this.textWriter.Write('"');
            }
        }

        private void WriteSymbolText(string text)
        {
            if (this.options.SymbolAsString)
            {
                this.textWriter.WriteString(text);
                return;
            }

            // Determine the variant for writing.
            SymbolVariant symbolVariant = GetSymbolVariant(text);

            switch (symbolVariant)
            {
                case SymbolVariant.Identifier:
                    this.textWriter.Write(text);
                    break;
                case SymbolVariant.Operator:
                    if (this.containerStack.Count > 0 && this.containerStack.Peek().containerType == IonType.Sexp)
                    {
                        this.textWriter.Write(text);
                        break;
                    }

                    goto case SymbolVariant.Quoted;
                case SymbolVariant.Quoted:
                    this.textWriter.WriteSingleQuotedSymbol(text);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(symbolVariant), symbolVariant, null);
            }
        }

        private void WriteFieldNameToken(SymbolToken token)
        {
            if (token.Text == null)
            {
                // unknown text, write id
                this.WriteSidLiteral(token.Sid);
                return;
            }

            this.WriteSymbolText(token.Text);
        }

        private void WriteAnnotations()
        {
            foreach (var annotation in this.annotations)
            {
                this.WriteAnnotationToken(annotation);
                this.textWriter.Write("::");
            }
        }

        private void WriteAnnotationToken(SymbolToken annotation)
        {
            var text = annotation.Text;
            if (text is null)
            {
                // unknown text, write sid
                this.textWriter.Write('$');
                this.textWriter.Write(annotation.Sid < 0 ? 0 : annotation.Sid);
                return;
            }

            var needQuote = GetSymbolVariant(text) != SymbolVariant.Identifier;

            if (needQuote)
            {
                this.textWriter.WriteSingleQuotedSymbol(text);
                return;
            }

            this.textWriter.WriteSymbol(text);
        }

        /// <summary>
        /// Write a separator between values.
        /// </summary>
        /// <param name="followingLongString">If this is a separator to separate long string.</param>
        /// <returns>If long string separation continues.</returns>
        private bool WriteSeparator(bool followingLongString)
        {
            if (this.options.PrettyPrint)
            {
                if (this.pendingSeparator && this.separatorCharacter > ' ')
                {
                    // Only bother if the separator is non-whitespace.
                    this.textWriter.Write(this.separatorCharacter);
                    followingLongString = false;
                }

                if (this.valueStarted)
                {
                    // this means we've written something
                    this.textWriter.Write(this.options.LineSeparator);
                }

                this.WriteLeadingWhiteSpace();
            }
            else if (this.pendingSeparator)
            {
                this.textWriter.Write(this.separatorCharacter);
                if (this.separatorCharacter > ' ')
                {
                    followingLongString = false;
                }
            }

            return followingLongString;
        }

        /// <summary>
        /// Write all the imported symbol tables.
        /// </summary>
        private void WriteImports()
        {
            // only write local symtab if we import sth more than just system table
            if (this.symbolTable.Imports.Count <= 1)
            {
                return;
            }

            this.AddTypeAnnotation(SystemSymbols.IonSymbolTable);
            this.StepIn(IonType.Struct);
            this.SetFieldName(SystemSymbols.Imports);
            this.StepIn(IonType.List);

            foreach (var importedTable in this.symbolTable.Imports)
            {
                if (importedTable.IsSystem)
                {
                    continue;
                }

                this.WriteImportTable(importedTable);
            }

            this.StepOut();
            this.StepOut();
        }

        private void CloseValue()
        {
            this.pendingSeparator = true;
            this.followingLongString = false;

            // TODO: Flush if a top-level-value was written
        }
    }
}
