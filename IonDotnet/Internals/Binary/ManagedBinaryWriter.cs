using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using IonDotnet.Utils;

namespace IonDotnet.Internals.Binary
{
    internal sealed class ManagedBinaryWriter : PrivateIonWriterBase
    {
        private sealed class PagedWriter256Buffer : PagedWriterBuffer
        {
            public PagedWriter256Buffer() : base(512)
            {
            }
        }

        private enum SymbolState
        {
            SystemSymbols,
            LocalSymbolsWithImportsOnly,
            LocalSymbols,
            LocalSymbolsFlushed
        }

        private class ImportedSymbolsContext
        {
            private readonly Dictionary<string, int> _dict = new Dictionary<string, int>();

            public readonly ISymbolTable[] Parents;
            public readonly int LocalSidStart;

            public ImportedSymbolsContext(ISymbolTable[] imports)
            {
                Parents = imports;

                //add all the system symbols
                LocalSidStart = SystemSymbols.Ion10MaxId + 1;
                foreach (var symbolTable in imports)
                {
                    if (symbolTable.IsShared) throw new IonException("Import table cannot be shared");
                    if (symbolTable.IsSystem) continue;

                    var declaredSymbols = symbolTable.IterateDeclaredSymbolNames();
                    while (declaredSymbols.HasNext())
                    {
                        var text = declaredSymbols.Next();
                        if (text != null)
                        {
                            _dict.TryAdd(text, LocalSidStart);
                        }

                        LocalSidStart++;
                    }
                }
            }

            public bool TryGetValue(string text, out int val)
            {
                val = default;

                if (text == null) return false;
                var systemTab = SharedSymbolTable.GetSystem(1);
                var st = systemTab.Find(text);
                if (st.Text != null)
                {
                    //found it
                    val = st.Sid;
                    return true;
                }

//                for (int i = 0, l = Symbols.SystemSymbolTokens.Length; i < l; i++)
//                {
//                    var systemToken = Symbols.SystemSymbolTokens[i];
//                    if (systemToken.Text != text) continue;
//                    val = systemToken.Sid;
//                    return true;
//                }

                if (Parents.Length == 0)
                    return false;

                return _dict.TryGetValue(text, out val);
            }
        }

        private readonly IDictionary<string, int> _locals;
        private bool _localsLocked;

        private readonly RawBinaryWriter _symbolsWriter;
        private readonly RawBinaryWriter _userWriter;
        private LocalSymbolTableView _localSymbolTableView;
        private readonly ImportedSymbolsContext _importContext;
        private SymbolState _symbolState;
        private readonly Stream _outputStream;

        public ManagedBinaryWriter(Stream outputStream, ISymbolTable[] importedTables)
        {
            if (!outputStream.CanWrite)
                throw new ArgumentException("Output stream must be writable", nameof(outputStream));

            _outputStream = outputStream;
            //raw writers and their buffers
            var lengthWriterBuffer = new PagedWriter256Buffer();
            var lengthSegment = new List<Memory<byte>>(2);
            _symbolsWriter = new RawBinaryWriter(lengthWriterBuffer, new PagedWriter256Buffer(), lengthSegment);
            _userWriter = new RawBinaryWriter(lengthWriterBuffer, new PagedWriter256Buffer(), lengthSegment);

            _importContext = new ImportedSymbolsContext(importedTables);
            _locals = new Dictionary<string, int>();
        }

        /// <summary>
        /// Only runs if the symbol state is SystemSymbol. Basically this will write the version marker,
        /// write all imported table names, and move to the local symbols
        /// </summary>
        /// <param name="writeIvm">Whether to write the Ion version marker</param>
        private void StartLocalSymbolTableIfNeeded(bool writeIvm)
        {
            if (_symbolState != SymbolState.SystemSymbols) return;

            if (writeIvm)
            {
                _symbolsWriter.WriteIonVersionMarker();
            }

            _symbolsWriter.AddTypeAnnotationSymbol(Symbols.GetSystemSymbol(SystemSymbols.IonSymbolTableSid));

            _symbolsWriter.StepIn(IonType.Struct); // $ion_symbol_table:{}
            if (_importContext.Parents.Length > 0)
            {
                _symbolsWriter.SetFieldNameSymbol(Symbols.GetSystemSymbol(SystemSymbols.ImportsSid));
                _symbolsWriter.StepIn(IonType.List); // $imports: []

                foreach (var importedTable in _importContext.Parents)
                {
                    _symbolsWriter.StepIn(IonType.Struct); // {name:'a', version: 1, max_id: 33}
                    _symbolsWriter.SetFieldNameSymbol(Symbols.GetSystemSymbol(SystemSymbols.NameSid));
                    _symbolsWriter.WriteString(importedTable.Name);
                    _symbolsWriter.SetFieldNameSymbol(Symbols.GetSystemSymbol(SystemSymbols.VersionSid));
                    _symbolsWriter.WriteInt(importedTable.Version);
                    _symbolsWriter.SetFieldNameSymbol(Symbols.GetSystemSymbol(SystemSymbols.MaxIdSid));
                    _symbolsWriter.WriteInt(importedTable.MaxId);
                    _symbolsWriter.StepOut();
                }

                _symbolsWriter.StepOut(); // $imports: []
            }

            _symbolState = SymbolState.LocalSymbolsWithImportsOnly;
        }

        /// <summary>
        /// Only run if symbolState is LocalSymbolsWithImportsOnly. This will start the list of local symbols
        /// </summary>
        private void StartLocalSymbolListIfNeeded()
        {
            if (_symbolState != SymbolState.LocalSymbolsWithImportsOnly) return;

            _symbolsWriter.SetFieldNameSymbol(Symbols.GetSystemSymbol(SystemSymbols.SymbolsSid));
            _symbolsWriter.StepIn(IonType.List); // symbols: []
            _symbolState = SymbolState.LocalSymbols;
        }

        /// <summary>
        /// Try intern a text into the symbols list, if the text is not in there already
        /// </summary>
        /// <param name="text">Text to intern</param>
        /// <returns>Corresponding token</returns>
        private SymbolToken Intern(string text)
        {
            Debug.Assert(text != null);

            var foundInImported = _importContext.TryGetValue(text, out var tokenSid);
            if (foundInImported)
            {
                if (tokenSid > SystemSymbols.Ion10MaxId)
                {
                    StartLocalSymbolTableIfNeeded(true);
                }

                return new SymbolToken(text, tokenSid);
            }

            //try the locals
            var foundInLocal = _locals.TryGetValue(text, out tokenSid);
            if (foundInLocal) return new SymbolToken(text, tokenSid);

            //try adding the text to the locals
            if (_localsLocked) throw new IonException("Local table is made read-only");

            StartLocalSymbolTableIfNeeded(true);
            StartLocalSymbolListIfNeeded();

            //progressively set the new sid
            tokenSid = _importContext.LocalSidStart + _locals.Count;
            _locals.Add(text, tokenSid);

            //write the new symbol to the list
            _symbolsWriter.WriteString(text);

            return new SymbolToken(text, tokenSid);
        }

        /// <summary>
        /// Try to intern the text of this token to our symbol table. 
        /// </summary>
        private SymbolToken InternSymbol(SymbolToken token)
        {
            if (token == default)
                return token;

            if (token.Text != null)
                return Intern(token.Text);

            //no text, check if sid is sth we know
            if (token.Sid > SymbolTable.MaxId)
                throw new UnknownSymbolException(token.Sid);

            return token;
        }

        public override ISymbolTable SymbolTable => _localSymbolTableView ?? (_localSymbolTableView = new LocalSymbolTableView(this));

        /// <inheritdoc />
        /// <summary>
        /// This is supposed to close the writer and release all their resources
        /// </summary>
        public override void Dispose()
        {
            var lengthBuffer = _userWriter?.GetLengthBuffer();
            Debug.Assert(lengthBuffer == _symbolsWriter.GetLengthBuffer());
            lengthBuffer?.Dispose();

            _userWriter?.Dispose();
            _symbolsWriter?.Dispose();
        }

//        public override async Task FlushAsync()
//        {
//            if (!PrepareFlush())
//                return;
//
//            var sLength = _symbolsWriter.PrepareFlush();
//            var uLength = _userWriter.PrepareFlush();
//
//            if (_outputStream is MemoryStream memoryStream)
//            {
//                var tLength = sLength + uLength;
//                memoryStream.Capacity += tLength;
//            }
//
//            await _symbolsWriter.FlushAsync(_outputStream);
//            await _userWriter.FlushAsync(_outputStream);
//
//            AfterFlush();
//        }

        /// <summary>
        /// Implementation should be such that this can be called many times
        /// </summary>
        public override void Flush()
        {
            if (!PrepareFlush())
                return;

            var sLength = _symbolsWriter.PrepareFlush();
            var uLength = _userWriter.PrepareFlush();

            if (_outputStream is MemoryStream memoryStream)
            {
                var tLength = sLength + uLength;
                memoryStream.Capacity += tLength;
            }

            _symbolsWriter.Flush(_outputStream);
            _userWriter.Flush(_outputStream);

            AfterFlush();
        }

        public override void WriteIonVersionMarker()
        {
            //do nothing, Ivm is always written
        }

        private bool PrepareFlush()
        {
            if (_userWriter.GetDepth() != 0)
                return false;

            switch (_symbolState)
            {
                case SymbolState.SystemSymbols:
                    _symbolsWriter.WriteIonVersionMarker();
                    break;
                case SymbolState.LocalSymbolsWithImportsOnly:
                    _symbolsWriter.StepOut();
                    break;
                case SymbolState.LocalSymbols:
                    _symbolsWriter.StepOut();
                    _symbolsWriter.StepOut();
                    break;
                case SymbolState.LocalSymbolsFlushed:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _symbolState = SymbolState.LocalSymbolsFlushed;

            return true;
        }

        /// <summary>
        /// This is called after flush() and will reset the raw writers. 
        /// </summary>
        private void AfterFlush()
        {
            _symbolsWriter.Reset();
            _userWriter.Reset();
        }

        public override void Finish()
        {
            if (_userWriter.GetDepth() != 0)
                throw new IonException($"Cannot finish writing at depth {_userWriter.GetDepth()}");

            //try to flush, writers' states are reset
            Flush();

            //finish() reset local symbols, and symbolState back to SystemSymbols
            _locals.Clear();
            _localsLocked = false;
            _symbolState = SymbolState.SystemSymbols;
        }

//        public override async Task FinishAsync()
//        {
//            if (_userWriter.GetDepth() != 0)
//                throw new IonException($"Cannot finish writing at depth {_userWriter.GetDepth()}");
//
//            //try to flush, writers' states are reset
//            await FlushAsync();
//
//            //finish() reset local symbols, and symbolState back to SystemSymbols
//            _locals.Clear();
//            _localsLocked = false;
//            _symbolState = SymbolState.SystemSymbols;
//        }

        public override void SetFieldName(string name)
        {
            if (!IsInStruct) throw new IonException("Cannot set a field name if the current container is not struct");
            if (name == null) throw new ArgumentNullException(nameof(name));

            var token = Intern(name);
            _userWriter.SetFieldNameSymbol(token);
        }

        public override void SetFieldNameSymbol(SymbolToken symbol)
        {
            var token = InternSymbol(symbol);
            _userWriter.SetFieldNameSymbol(token);
        }

        public override void StepIn(IonType type)
        {
            // TODO implement top-level symbol table
            _userWriter.StepIn(type);
        }

        public override void StepOut()
        {
            // TODO implement top-level symbol table
            _userWriter.StepOut();
        }

        public override bool IsInStruct => _userWriter.IsInStruct;

        public override void WriteNull()
        {
            _userWriter.WriteNull();
        }

        public override void WriteNull(IonType type)
        {
            _userWriter.WriteNull(type);
        }

        public override void WriteBool(bool value)
        {
            _userWriter.WriteBool(value);
        }

        public override void WriteInt(long value)
        {
            _userWriter.WriteInt(value);
        }

        public override void WriteInt(BigInteger value)
        {
            _userWriter.WriteInt(value);
        }

        public override void WriteFloat(double value)
        {
            _userWriter.WriteFloat(value);
        }

        public override void WriteDecimal(decimal value)
        {
            _userWriter.WriteDecimal(value);
        }

        public override void WriteTimestamp(Timestamp value)
        {
            _userWriter.WriteTimestamp(value);
        }

        public override void WriteSymbol(string symbol)
        {
            var token = Intern(symbol);
            _userWriter.WriteSymbolToken(token);
        }

        public override void WriteSymbolToken(SymbolToken symbolToken)
        {
            symbolToken = InternSymbol(symbolToken);
            if (symbolToken != default
                && symbolToken.Sid == SystemSymbols.Ion10Sid
                && _userWriter.GetDepth() == 0
                && _userWriter._annotations.Count == 0)
            {
                //this is an ivm
                Finish();
                return;
            }

            _userWriter.WriteSymbolToken(symbolToken);
        }

        public override void WriteString(string value)
        {
            _userWriter.WriteString(value);
        }

        public override void WriteBlob(ReadOnlySpan<byte> value) => _userWriter.WriteBlob(value);

        public override void WriteClob(ReadOnlySpan<byte> value) => _userWriter.WriteClob(value);

        public override void SetTypeAnnotation(string annotation)
        {
            if (annotation == default)
                throw new ArgumentNullException(nameof(annotation));

            _userWriter.ClearAnnotations();
            var token = Intern(annotation);
            _userWriter.AddTypeAnnotationSymbol(token);
        }

        public override void SetTypeAnnotations(IEnumerable<string> annotations)
        {
            if (annotations == null)
                throw new ArgumentNullException(nameof(annotations));
            _userWriter.ClearAnnotations();
            foreach (var annotation in annotations)
            {
                var token = Intern(annotation);
                _userWriter.AddTypeAnnotationSymbol(token);
            }
        }

        public override bool IsFieldNameSet() => _userWriter.IsFieldNameSet();

        public override int GetDepth() => _userWriter.GetDepth();

        public override void AddTypeAnnotation(string annotation)
        {
            var token = Intern(annotation);
            _userWriter.AddTypeAnnotationSymbol(token);
        }

        /// <summary>
        /// Reflects the 'view' of the local symbol used in this writer
        /// </summary>
        private class LocalSymbolTableView : AbstractSymbolTable
        {
            private readonly ManagedBinaryWriter _writer;

            public LocalSymbolTableView(ManagedBinaryWriter writer) : base(string.Empty, 0)
            {
                _writer = writer;
            }

            public override bool IsLocal => true;
            public override bool IsShared => false;
            public override bool IsSubstitute => false;
            public override bool IsSystem => false;
            public override bool IsReadOnly => _writer._localsLocked;

            public override void MakeReadOnly() => _writer._localsLocked = true;

            public override ISymbolTable GetSystemTable() => SharedSymbolTable.GetSystem(1);

            public override IEnumerable<ISymbolTable> GetImportedTables() => _writer._importContext.Parents;

            public override int GetImportedMaxId() => _writer._importContext.LocalSidStart - 1;

            public override int MaxId => GetImportedMaxId() + _writer._locals.Count;

            public override SymbolToken Intern(string text)
            {
                var existing = Find(text);
                if (existing != default) return existing;
                if (IsReadOnly) throw new InvalidOperationException("Table is read-only");

                return _writer.Intern(text);
            }

            public override SymbolToken Find(string text)
            {
                if (text == null) throw new ArgumentNullException(nameof(text));

                var found = _writer._importContext.TryGetValue(text, out var sid);
                if (found) return new SymbolToken(text, sid);
                found = _writer._locals.TryGetValue(text, out sid);

                return found ? new SymbolToken(text, sid) : default;
            }

            public override string FindKnownSymbol(int sid)
            {
                foreach (var symbolTable in _writer._importContext.Parents)
                {
                    var text = symbolTable.FindKnownSymbol(sid);
                    if (text == null) continue;
                    return text;
                }

                return _writer._locals.FirstOrDefault(kvp => kvp.Value == sid).Key;
            }

            public override IIterator<string> IterateDeclaredSymbolNames() => new PeekIterator<string>(_writer._locals.Keys);
        }
    }
}
