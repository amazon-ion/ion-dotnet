using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using IonDotnet.Utils;

namespace IonDotnet.Internals
{
    internal class ManagedBinaryWriter : IIonWriter
    {
        private class PagedWriter512Buffer : PagedWriterBuffer
        {
            public PagedWriter512Buffer() : base(512)
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
            public readonly List<ISymbolTable> Parents;
            public readonly IDictionary<string, int> SymbolResolver = new Dictionary<string, int>();
            public readonly int LocalSidStart;

            public ImportedSymbolsContext(IReadOnlyCollection<ISymbolTable> imports)
            {
                Parents = new List<ISymbolTable>(imports.Count);
                //add all the system symbols
                foreach (var systemSymbolToken in Symbols.SystemSymbolTokens)
                {
                    SymbolResolver.Add(systemSymbolToken.Text, systemSymbolToken.Sid);
                }

                LocalSidStart = SystemSymbols.Ion10MaxId + 1;
                foreach (var symbolTable in imports)
                {
                    if (symbolTable.IsShared) throw new IonException("Import table cannot be shared");
                    if (symbolTable.IsSystem) continue;

                    Parents.Add(symbolTable);

                    var declaredSymbols = symbolTable.IterateDeclaredSymbolNames();
                    while (declaredSymbols.HasNext())
                    {
                        var text = declaredSymbols.Next();
                        if (text != null && !SymbolResolver.ContainsKey(text))
                        {
                            SymbolResolver.Add(text, LocalSidStart);
                        }

                        LocalSidStart++;
                    }
                }
            }
        }

        private readonly IDictionary<string, int> _locals;
        private bool _localsLocked = false;

        private readonly RawBinaryWriter _symbolsWriter;
        private readonly RawBinaryWriter _userWriter;

        private readonly ImportedSymbolsContext _importContext;

        private readonly Stream _outputStream;

        private SymbolState _symbolState;

        public ManagedBinaryWriter(Stream outputStream, IReadOnlyCollection<ISymbolTable> importedTables)
        {
            if (!outputStream.CanWrite) throw new ArgumentException("Output stream must be writable", nameof(outputStream));
            _outputStream = outputStream;

            var lengthWriterBuffer = new PagedWriter512Buffer();

            _symbolsWriter = new RawBinaryWriter(lengthWriterBuffer, new PagedWriter512Buffer());
            _userWriter = new RawBinaryWriter(lengthWriterBuffer, new PagedWriter512Buffer());
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
            if (_importContext.Parents.Count > 0)
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

            var foundInImported = _importContext.SymbolResolver.TryGetValue(text, out var tokenSid);
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
            if (!foundInLocal)
            {
                //try adding the text to the locals
                if (_localsLocked) throw new IonException("Local table is made read-only");

                StartLocalSymbolTableIfNeeded(true);
                StartLocalSymbolListIfNeeded();

                //progressively set the new sid
                tokenSid = _importContext.LocalSidStart + _locals.Count;
                _locals.Add(text, tokenSid);

                //write the new symbol to the list
                _symbolsWriter.WriteString(text);
            }

            return new SymbolToken(text, tokenSid);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public ISymbolTable SymbolTable { get; }

        public void Flush()
        {
            if (_userWriter.GetDepth() != 0) return;

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
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _symbolState = SymbolState.LocalSymbolsFlushed;

            _symbolsWriter.WriteTo(_outputStream);
            _userWriter.WriteTo(_outputStream);
        }

        public void Finish()
        {
            if (_userWriter.GetDepth() != 0) throw new IonException($"Cannot finish writing at depth {_userWriter.GetDepth()}");
            Flush();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void SetFieldName(string name)
        {
            if (!IsInStruct) throw new IonException("Cannot set a field name if the current container is not struct");
            if (name == null) throw new ArgumentNullException(nameof(name));

            var token = Intern(name);
            _userWriter.SetFieldNameSymbol(token);
        }

        public void SetFieldNameSymbol(SymbolToken name)
        {
            throw new NotImplementedException();
        }

        public void StepIn(IonType type)
        {
            // TODO implement top-level symbol table
            _userWriter.StepIn(type);
        }

        public void StepOut()
        {
            // TODO implement top-level symbol table
            _userWriter.StepOut();
        }

        public bool IsInStruct => _userWriter.IsInStruct;

        public void WriteValue(IIonReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteValues(IIonReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteNull()
        {
            _userWriter.WriteNull();
        }

        public void WriteNull(IonType type)
        {
            _userWriter.WriteNull(type);
        }

        public void WriteBool(bool value)
        {
            _userWriter.WriteBool(value);
        }

        public void WriteInt(long value)
        {
            _userWriter.WriteInt(value);
        }

        public void WriteInt(BigInteger value)
        {
            _userWriter.WriteInt(value);
        }

        public void WriteFloat(double value)
        {
            _userWriter.WriteFloat(value);
        }

        public void WriteDecimal(decimal value)
        {
            _userWriter.WriteDecimal(value);
        }

        public void WriteTimestamp(DateTime value)
        {
            _userWriter.WriteTimestamp(value);
        }

        public void WriteSymbol(SymbolToken symbolToken)
        {
            _userWriter.WriteSymbol(symbolToken);
        }

        public void WriteString(string value)
        {
            _userWriter.WriteString(value);
        }

        public void WriteBlob(byte[] value)
        {
            throw new NotImplementedException();
        }

        public void WriteBlob(ArraySegment<byte> value)
        {
            throw new NotImplementedException();
        }

        public void WriteClob(byte[] value)
        {
            throw new NotImplementedException();
        }

        public void WriteClob(ArraySegment<byte> value)
        {
            throw new NotImplementedException();
        }

        public void SetTypeAnnotations(params string[] annotations)
        {
            throw new NotImplementedException();
        }

        public void SetTypeAnnotationSymbols(ArraySegment<SymbolToken> annotations)
        {
            throw new NotImplementedException();
        }

        public void AddTypeAnnotation(string annotation)
        {
            var token = Intern(annotation);
            _userWriter.AddTypeAnnotationSymbol(token);
        }
    }
}
