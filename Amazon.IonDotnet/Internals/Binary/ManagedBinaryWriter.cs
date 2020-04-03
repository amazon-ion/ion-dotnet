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
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using Amazon.IonDotnet.Utils;

    internal sealed class ManagedBinaryWriter : PrivateIonWriterBase
    {
        private readonly IDictionary<string, int> locals;
        private readonly RawBinaryWriter symbolsWriter;
        private readonly RawBinaryWriter userWriter;
        private readonly ImportedSymbolsContext importContext;
        private readonly Stream outputStream;
        private bool localsLocked;
        private LocalSymbolTableView localSymbolTableView;
        private SymbolState symbolState;

        public ManagedBinaryWriter(
            Stream outputStream,
            IEnumerable<ISymbolTable> importedTables,
            bool forceFloat64 = false)
        {
            if (!outputStream.CanWrite)
            {
                throw new ArgumentException("Output stream must be writable", nameof(outputStream));
            }

            this.outputStream = outputStream;

            // raw writers and their buffers
            var lengthWriterBuffer = new PagedWriter256Buffer();
            var lengthSegment = new List<Memory<byte>>(2);
            this.symbolsWriter = new RawBinaryWriter(
                lengthWriterBuffer,
                new PagedWriter256Buffer(),
                lengthSegment,
                forceFloat64);
            this.userWriter = new RawBinaryWriter(
                lengthWriterBuffer,
                new PagedWriter256Buffer(),
                lengthSegment,
                forceFloat64);

            this.importContext = new ImportedSymbolsContext(importedTables);
            this.locals = new Dictionary<string, int>();
        }

        private enum SymbolState
        {
            SystemSymbols,
            LocalSymbolsWithImportsOnly,
            LocalSymbols,
            LocalSymbolsFlushed,
        }

        public override ISymbolTable SymbolTable => this.localSymbolTableView ?? (this.localSymbolTableView = new LocalSymbolTableView(this));

        public override bool IsInStruct => this.userWriter.IsInStruct;

        public override void AddTypeAnnotationSymbol(SymbolToken annotation)
        {
            var token = this.InternSymbol(annotation);
            this.userWriter.AddTypeAnnotationSymbol(token);
        }

        public override void ClearTypeAnnotations() => this.userWriter.ClearTypeAnnotations();

        /// <inheritdoc />
        /// <summary>
        /// This is supposed to close the writer and release all their resources.
        /// </summary>
        public override void Dispose()
        {
            var lengthBuffer = this.userWriter?.GetLengthBuffer();
            Debug.Assert(lengthBuffer == this.symbolsWriter.GetLengthBuffer(), "lengthBuffers do not match");
            lengthBuffer?.Dispose();

            this.userWriter?.Dispose();
            this.symbolsWriter?.Dispose();
        }

        /// <summary>
        /// Implementation should be such that this can be called many times.
        /// </summary>
        public override void Flush()
        {
            if (!this.PrepareFlush())
            {
                return;
            }

            var sLength = this.symbolsWriter.PrepareFlush();
            var uLength = this.userWriter.PrepareFlush();

            if (this.outputStream is MemoryStream memoryStream)
            {
                var tLength = sLength + uLength;
                memoryStream.Capacity += tLength;
            }

            this.symbolsWriter.Flush(this.outputStream);
            this.userWriter.Flush(this.outputStream);

            this.AfterFlush();
        }

        public override void WriteIonVersionMarker()
        {
            // do nothing, Ivm is always written
        }

        public override void Finish()
        {
            if (this.userWriter.GetDepth() != 0)
            {
                throw new IonException($"Cannot finish writing at depth {this.userWriter.GetDepth()}");
            }

            // try to flush, writers' states are reset
            this.Flush();

            // finish() reset local symbols, and symbolState back to SystemSymbols
            this.locals.Clear();
            this.localsLocked = false;
            this.symbolState = SymbolState.SystemSymbols;
        }

        public override void SetFieldName(string name)
        {
            if (!this.IsInStruct)
            {
                throw new IonException("Cannot set a field name if the current container is not struct");
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var token = this.Intern(name);
            this.userWriter.SetFieldNameSymbol(token);
        }

        public override void SetFieldNameSymbol(SymbolToken symbol)
        {
            var token = this.InternSymbol(symbol);
            this.userWriter.SetFieldNameSymbol(token);
        }

        public override void StepIn(IonType type)
        {
            // TODO implement top-level symbol table
            this.userWriter.StepIn(type);
        }

        public override void StepOut()
        {
            // TODO implement top-level symbol table
            this.userWriter.StepOut();
        }

        public override void WriteNull()
        {
            this.userWriter.WriteNull();
        }

        public override void WriteNull(IonType type)
        {
            this.userWriter.WriteNull(type);
        }

        public override void WriteBool(bool value)
        {
            this.userWriter.WriteBool(value);
        }

        public override void WriteInt(long value)
        {
            this.userWriter.WriteInt(value);
        }

        public override void WriteInt(BigInteger value)
        {
            this.userWriter.WriteInt(value);
        }

        public override void WriteFloat(double value)
        {
            this.userWriter.WriteFloat(value);
        }

        public override void WriteDecimal(decimal value)
        {
            this.userWriter.WriteDecimal(value);
        }

        public override void WriteDecimal(BigDecimal value)
        {
            this.userWriter.WriteDecimal(value);
        }

        public override void WriteTimestamp(Timestamp value)
        {
            this.userWriter.WriteTimestamp(value);
        }

        public override void WriteSymbol(string symbol)
        {
            var token = this.Intern(symbol);
            this.userWriter.WriteSymbolToken(token);
        }

        public override void WriteSymbolToken(SymbolToken symbolToken)
        {
            symbolToken = this.InternSymbol(symbolToken);
            if (symbolToken != default
                && symbolToken.Sid == SystemSymbols.Ion10Sid
                && this.userWriter.GetDepth() == 0
                && this.userWriter.Annotations.Count == 0)
            {
                // this is an ivm
                this.Finish();
                return;
            }

            this.userWriter.WriteSymbolToken(symbolToken);
        }

        public override void WriteString(string value)
        {
            this.userWriter.WriteString(value);
        }

        public override void WriteBlob(ReadOnlySpan<byte> value) => this.userWriter.WriteBlob(value);

        public override void WriteClob(ReadOnlySpan<byte> value) => this.userWriter.WriteClob(value);

        public override void SetTypeAnnotations(IEnumerable<string> annotations)
        {
            if (annotations == null)
            {
                throw new ArgumentNullException(nameof(annotations));
            }

            this.userWriter.ClearTypeAnnotations();
            foreach (var annotation in annotations)
            {
                var token = this.Intern(annotation);
                this.userWriter.AddTypeAnnotationSymbol(token);
            }
        }

        public override bool IsFieldNameSet() => this.userWriter.IsFieldNameSet();

        public override int GetDepth() => this.userWriter.GetDepth();

        public override void AddTypeAnnotation(string annotation)
        {
            var token = this.Intern(annotation);
            this.userWriter.AddTypeAnnotationSymbol(token);
        }

        /// <summary>
        /// Only runs if the symbol state is SystemSymbol. Basically this will write the version marker,
        /// write all imported table names, and move to the local symbols.
        /// </summary>
        /// <param name="writeIvm">Whether to write the Ion version marker.</param>
        private void StartLocalSymbolTableIfNeeded(bool writeIvm)
        {
            if (this.symbolState != SymbolState.SystemSymbols)
            {
                return;
            }

            if (writeIvm)
            {
                this.symbolsWriter.WriteIonVersionMarker();
            }

            this.symbolsWriter.AddTypeAnnotationSymbol(Symbols.GetSystemSymbol(SystemSymbols.IonSymbolTableSid));

            this.symbolsWriter.StepIn(IonType.Struct); // $ion_symbol_table:{}
            if (this.importContext.Parents.Length > 0)
            {
                this.symbolsWriter.SetFieldNameSymbol(Symbols.GetSystemSymbol(SystemSymbols.ImportsSid));
                this.symbolsWriter.StepIn(IonType.List); // $imports: []

                foreach (var importedTable in this.importContext.Parents)
                {
                    this.symbolsWriter.WriteImportTable(importedTable);
                }

                this.symbolsWriter.StepOut(); // $imports: []
            }

            this.symbolState = SymbolState.LocalSymbolsWithImportsOnly;
        }

        /// <summary>
        /// Only run if symbolState is LocalSymbolsWithImportsOnly. This will start the list of local symbols.
        /// </summary>
        private void StartLocalSymbolListIfNeeded()
        {
            if (this.symbolState != SymbolState.LocalSymbolsWithImportsOnly)
            {
                return;
            }

            this.symbolsWriter.SetFieldNameSymbol(Symbols.GetSystemSymbol(SystemSymbols.SymbolsSid));
            this.symbolsWriter.StepIn(IonType.List); // symbols: []
            this.symbolState = SymbolState.LocalSymbols;
        }

        /// <summary>
        /// Try intern a text into the symbols list, if the text is not in there already.
        /// </summary>
        /// <param name="text">Text to intern.</param>
        /// <returns>Corresponding token.</returns>
        private SymbolToken Intern(string text)
        {
            Debug.Assert(text != null, "text is null");

            var foundInImported = this.importContext.TryGetValue(text, out var tokenSid);
            if (foundInImported)
            {
                if (tokenSid > SystemSymbols.Ion10MaxId)
                {
                    this.StartLocalSymbolTableIfNeeded(true);
                }

                return new SymbolToken(text, tokenSid);
            }

            // try the locals
            var foundInLocal = this.locals.TryGetValue(text, out tokenSid);
            if (foundInLocal)
            {
                return new SymbolToken(text, tokenSid);
            }

            // try adding the text to the locals
            if (this.localsLocked)
            {
                throw new IonException("Local table is made read-only");
            }

            this.StartLocalSymbolTableIfNeeded(true);
            this.StartLocalSymbolListIfNeeded();

            // progressively set the new sid
            tokenSid = this.importContext.LocalSidStart + this.locals.Count;
            this.locals.Add(text, tokenSid);

            // write the new symbol to the list
            this.symbolsWriter.WriteString(text);

            return new SymbolToken(text, tokenSid);
        }

        /// <summary>
        /// Try to intern the text of this token to our symbol table.
        /// </summary>
        private SymbolToken InternSymbol(SymbolToken token)
        {
            if (token == default)
            {
                return token;
            }

            if (token.Text != null)
            {
                return this.Intern(token.Text);
            }

            // no text, check if sid is sth we know
            if (token.Sid > this.SymbolTable.MaxId)
            {
                throw new UnknownSymbolException(token.Sid);
            }

            return token;
        }

        private bool PrepareFlush()
        {
            if (this.userWriter.GetDepth() != 0)
            {
                return false;
            }

            switch (this.symbolState)
            {
                case SymbolState.SystemSymbols:
                    this.symbolsWriter.WriteIonVersionMarker();
                    break;
                case SymbolState.LocalSymbolsWithImportsOnly:
                    this.symbolsWriter.StepOut();
                    break;
                case SymbolState.LocalSymbols:
                    this.symbolsWriter.StepOut();
                    this.symbolsWriter.StepOut();
                    break;
                case SymbolState.LocalSymbolsFlushed:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            this.symbolState = SymbolState.LocalSymbolsFlushed;

            return true;
        }

        /// <summary>
        /// This is called after flush() and will reset the raw writers.
        /// </summary>
        private void AfterFlush()
        {
            this.symbolsWriter.Reset();
            this.userWriter.Reset();
        }

        private sealed class PagedWriter256Buffer : PagedWriterBuffer
        {
            public PagedWriter256Buffer()
                : base(512)
            {
            }
        }

        private class ImportedSymbolsContext
        {
            private readonly Dictionary<string, int> dict = new Dictionary<string, int>();

            public ImportedSymbolsContext(IEnumerable<ISymbolTable> imports)
            {
                this.Parents = imports?.ToArray() ?? Symbols.EmptySymbolTablesArray;

                // add all the system symbols
                this.LocalSidStart = SystemSymbols.Ion10MaxId + 1;
                foreach (var symbolTable in this.Parents)
                {
                    if (!symbolTable.IsShared)
                    {
                        throw new IonException("Import table must be a shared table.");
                    }

                    if (symbolTable.IsSystem)
                    {
                        continue;
                    }

                    if (symbolTable.IsSubstitute)
                    {
                        var sTable = (SubstituteSymbolTable)symbolTable;
                        var oSymbols = sTable.GetOriginalSymbols();
                        var idStart = this.LocalSidStart;
                        foreach (var otext in oSymbols)
                        {
                            if (otext != null)
                            {
                                this.dict.TryAdd(otext, idStart);
                            }

                            idStart++;
                        }

                        this.LocalSidStart += symbolTable.MaxId;
                        continue;
                    }

                    var declaredSymbols = symbolTable.GetDeclaredSymbolNames();
                    foreach (var text in declaredSymbols)
                    {
                        if (text != null)
                        {
                            this.dict.TryAdd(text, this.LocalSidStart);
                        }

                        this.LocalSidStart++;
                    }
                }
            }

            public ISymbolTable[] Parents { get; }

            public int LocalSidStart { get; }

            public bool TryGetValue(string text, out int val)
            {
                if (text == null)
                {
                    val = 0;
                    return false;
                }

                var systemTab = SharedSymbolTable.GetSystem(1);
                val = systemTab.FindSymbolId(text);
                if (val > 0)
                {
                    return true;
                }

                return this.dict.TryGetValue(text, out val);
            }
        }

        /// <summary>
        /// Reflects the 'view' of the local symbol used in this writer.
        /// </summary>
        private class LocalSymbolTableView : ISymbolTable
        {
            private readonly ManagedBinaryWriter writer;

            public LocalSymbolTableView(ManagedBinaryWriter writer)
            {
                this.writer = writer;
            }

            public string Name => string.Empty;

            public int Version => 0;

            public bool IsLocal => true;

            public bool IsShared => false;

            public bool IsSubstitute => false;

            public bool IsSystem => false;

            public bool IsReadOnly => this.writer.localsLocked;

            public int MaxId => this.GetImportedMaxId() + this.writer.locals.Count;

            public string IonVersionId => SystemSymbols.Ion10;

            public void MakeReadOnly() => this.writer.localsLocked = true;

            public ISymbolTable GetSystemTable() => SharedSymbolTable.GetSystem(1);

            public IReadOnlyList<ISymbolTable> GetImportedTables() => this.writer.importContext.Parents;

            public int GetImportedMaxId() => this.writer.importContext.LocalSidStart - 1;

            public SymbolToken Intern(string text)
            {
                var existing = this.Find(text);
                if (existing != default)
                {
                    return existing;
                }

                if (this.IsReadOnly)
                {
                    throw new InvalidOperationException("Table is read-only");
                }

                return this.writer.Intern(text);
            }

            public SymbolToken Find(string text)
            {
                if (text == null)
                {
                    throw new ArgumentNullException(nameof(text));
                }

                var found = this.writer.importContext.TryGetValue(text, out _);
                if (found)
                {
                    return new SymbolToken(text, SymbolToken.UnknownSid);
                }

                found = this.writer.locals.TryGetValue(text, out _);

                return found ? new SymbolToken(text, SymbolToken.UnknownSid) : default;
            }

            public string FindKnownSymbol(int sid)
            {
                foreach (var symbolTable in this.writer.importContext.Parents)
                {
                    var text = symbolTable.FindKnownSymbol(sid);
                    if (text == null)
                    {
                        continue;
                    }

                    return text;
                }

                return this.writer.locals.FirstOrDefault(kvp => kvp.Value == sid).Key;
            }

            public IEnumerable<string> GetDeclaredSymbolNames() => this.writer.locals.Keys;

            public int FindSymbolId(string text)
            {
                SymbolToken token = this.Find(text);
                return token == default ? SymbolToken.UnknownSid : token.Sid;
            }

            public void WriteTo(IIonWriter writer)
            {
                throw new NotImplementedException();
            }
        }
    }
}
