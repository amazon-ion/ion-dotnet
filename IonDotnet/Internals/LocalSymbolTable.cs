using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using IonDotnet.Internals.Binary;
using IonDotnet.Utils;

namespace IonDotnet.Internals
{
    public class LocalSymbolTable : ISymbolTable
    {
        private readonly int _firstLocalId;
        private readonly LocalSymbolTableImports _imports;
        private readonly IList<string> _mySymbolNames;
        private readonly IDictionary<string, int> _symbolMap;

        private LocalSymbolTable(LocalSymbolTableImports imports, IList<string> symbolList, bool readOnly)
        {
            IsReadOnly = readOnly;
            _imports = imports;
            _firstLocalId = _imports.MaxId + 1;
            if (symbolList == null || symbolList.Count == 0)
            {
                _mySymbolNames = PrivateHelper.EmptyStringArray;
            }
            else
            {
                _mySymbolNames = symbolList;
            }

            _symbolMap = BuildSymbolMap();
        }

        private LocalSymbolTable(LocalSymbolTable copyFrom, int maxId)
        {
            IsReadOnly = false;
            _firstLocalId = copyFrom._firstLocalId;
            _imports = copyFrom._imports;
            var symbolCount = maxId - _imports.MaxId;

            //copy list
            _mySymbolNames = copyFrom._mySymbolNames.Take(symbolCount).ToList();
            _symbolMap = BuildSymbolMap();
        }

        private IDictionary<string, int> BuildSymbolMap()
        {
            IDictionary<string, int> map;
            var sid = _firstLocalId;
            if (IsReadOnly)
            {
                map = new Dictionary<string, int>();
            }
            else
            {
                map = new ConcurrentDictionary<string, int>();
            }

            for (var i = 0; i < _mySymbolNames.Count; i++, sid++)
            {
                var symbolText = _mySymbolNames[i];
                if (symbolText == null) continue; //shouldn't happen

                if (!map.ContainsKey(symbolText))
                {
                    map.Add(symbolText, sid);
                }
            }

            return map;
        }

        //local has no name
        public string Name => string.Empty;

        //and no version
        public int Version => 0;

        public bool IsLocal => true;
        public bool IsShared => false;
        public bool IsSubstitute => false;
        public bool IsSystem => false;
        public bool IsReadOnly { get; private set; }

        public void MakeReadOnly() => IsReadOnly = true;

        public ISymbolTable GetSystemTable() => _imports.SystemTable;

        public string IonVersionId => _imports.SystemTable.IonVersionId;

        public IEnumerable<ISymbolTable> GetImportedTables() => _imports.GetSymbolTables();

        public int GetImportedMaxId() => _imports.MaxId;

        public int MaxId => _mySymbolNames.Count + _imports.MaxId;

        public SymbolToken Intern(string text)
        {
            var token = Find(text);
            if (token != default)
                return token;
            //TODO validate symbol

            var sid = PutSymbol(text);
            return new SymbolToken(text, sid);
        }

        private int PutSymbol(string text)
        {
            if (IsReadOnly)
                throw new InvalidOperationException("Table is read-only");
            var sid = _mySymbolNames.Count + _firstLocalId;
            Debug.Assert(sid == MaxId + 1);
            Debug.Assert(!_symbolMap.ContainsKey(text));
            _symbolMap.Add(text, sid);
            _mySymbolNames.Add(text);
            return sid;
        }

        public SymbolToken Find(string text)
        {
            if (string.IsNullOrEmpty(text)) throw new ArgumentNullException(nameof(text));

            var token = _imports.Find(text);
            if (token != SymbolToken.None) return token;

            var found = _symbolMap.TryGetValue(text, out var sid);
            if (!found) return default;

            Debug.Assert(text == _mySymbolNames[sid - _firstLocalId]);
            return new SymbolToken(text, sid);
        }

        public int FindSymbol(string text)
        {
            var sid = _imports.FindSymbol(text);
            return sid != SymbolToken.UnknownSid ? sid : FindLocalSymbolPrivate(text);
        }

        private int FindLocalSymbolPrivate(string text)
        {
            return _symbolMap.TryGetValue(text, out var sid) ? sid : SymbolToken.UnknownSid;
        }

        public string FindKnownSymbol(int sid)
        {
            if (sid < 0) throw new ArgumentOutOfRangeException(nameof(sid), $"{nameof(sid)} must be >=0");

            if (sid < _firstLocalId) return _imports.FindKnownSymbol(sid);

            IList<string> names;
            //avoid locking if possible
            if (IsReadOnly)
            {
                names = _mySymbolNames;
            }
            else
            {
                lock (this)
                {
                    //this is to avoid array resizing effect, I guess
                    names = _mySymbolNames;
                }
            }

            var offset = sid - _firstLocalId;
            return offset < names.Count ? names[offset] : null;
        }

        public void WriteTo(IIonWriter writer) => writer.WriteValues(new SymbolTableReader(this));

        public IIterator<string> IterateDeclaredSymbolNames() => new PeekIterator<string>(_mySymbolNames);

        private static LocalSymbolTableImports ReadLocalSymbolTableImports(IIonReader reader, bool isOnStruct, out IList<string> symbolList)
        {
            IList<ISymbolTable> importList = null;
            symbolList = new List<string>();
            if (!isOnStruct)
            {
                reader.MoveNext();
            }

            Debug.Assert(reader.CurrentType == IonType.Struct);

            // assume that we're standing before a struct
            reader.StepIn();
            var foundImport = false;
            IonType fieldType;
            while ((fieldType = reader.MoveNext()) != IonType.None)
            {
                if (reader.CurrentIsNull) continue;

                var symtok = reader.GetFieldNameSymbol();
                if (symtok.Sid == SymbolToken.UnknownSid)
                {
                    throw new NotImplementedException();
                }

                switch (symtok.Sid)
                {
                    case SystemSymbols.ImportsSid:
                        if (importList == null)
                        {
                            importList = new List<ISymbolTable>();
                        }

                        //TODO what's next?
                        break;
                    case SystemSymbols.SymbolsSid:
                        if (foundImport) throw new IonException("Multiple symbols field");
                        foundImport = true;
                        if (fieldType != IonType.List) break;

                        ReadSymbolList(reader, symbolList);
                        break;
                }
            }

            reader.StepOut();
            return new LocalSymbolTableImports(SharedSymbolTable.GetSystem(1), importList);
        }

        private static void ReadSymbolList(IIonReader reader, ICollection<string> symbolList)
        {
            reader.StepIn();

            IonType type;
            while ((type = reader.MoveNext()) != IonType.None)
            {
                var text = type == IonType.String ? reader.StringValue() : null;
                if (text == null)
                {
                    Console.WriteLine(type);
                }

                symbolList.Add(text);
            }

            reader.StepOut();
        }

        /// <summary>
        /// Try to read the symbols used in this datagram
        /// </summary>
        /// <param name="reader">Datagram reader</param>
        /// <param name="isOnStruct">Reader is before the $ion_symbol_table struct</param>
        /// <returns>Local symbol table</returns>
        public static LocalSymbolTable Read(IIonReader reader, bool isOnStruct)
        {
            var imports = ReadLocalSymbolTableImports(reader, isOnStruct, out var symbolList);
            return new LocalSymbolTable(imports, symbolList, true);
        }

        public ISymbolTable Copy() => new LocalSymbolTable(this, MaxId);
    }
}
