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
            if (string.IsNullOrEmpty(text))
                throw new ArgumentNullException(nameof(text));

            var token = _imports.Find(text);
            if (token != SymbolToken.None)
                return token;

            var found = _symbolMap.TryGetValue(text, out var sid);
            if (!found)
                return default;

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
            if (sid < 0)
                throw new ArgumentOutOfRangeException(nameof(sid), $"{nameof(sid)} must be >=0");

            if (sid < _firstLocalId)
                return _imports.FindKnownSymbol(sid);

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

        private static LocalSymbolTableImports ReadLocalSymbolTableImports(IIonReader reader, ICatalog catalog, bool isOnStruct, out IList<string> symbolList)
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
            bool foundImport = false, foundLocals = false;
            IonType fieldType;
            while ((fieldType = reader.MoveNext()) != IonType.None)
            {
                if (reader.CurrentIsNull)
                    continue;

                var sid = reader.GetFieldNameSymbol().Sid;
                if (sid == SymbolToken.UnknownSid)
                {
                    //user-level symtab
                    sid = SystemSymbols.ResolveSidForSymbolTableField(reader.CurrentFieldName);
                }

                switch (sid)
                {
                    case SystemSymbols.ImportsSid:
                        if (foundImport)
                            throw new IonException("Multiple imports field");
                        foundImport = true;
                        importList = new List<ISymbolTable>();
                        if (fieldType == IonType.List)
                        {
                            //list of symbol tables to imports
                            ReadImportList(reader, catalog, importList);
                        }
                        else if (fieldType == IonType.Symbol
                                 && SystemSymbols.IonSymbolTable.Equals(reader.StringValue())
                                 && reader.GetSymbolTable().IsLocal)
                        {
                            //reader has a prev local symtab && current field is imports:$ion_symbol_table
                            //import the prev table
                            importList.Add(reader.GetSymbolTable());
                        }

                        break;
                    case SystemSymbols.SymbolsSid:
                        if (foundLocals)
                            throw new IonException("Multiple symbols field");
                        foundLocals = true;
                        if (fieldType != IonType.List)
                            break;

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
                if (text is null)
                    throw new IonException("Symbol must be a valid text");

                symbolList.Add(text);
            }

            reader.StepOut();
        }

        /// <summary>
        /// Collect the symbol tables in the reader from the catalog to the import list.
        /// </summary>
        private static void ReadImportList(IIonReader reader, ICatalog catalog, IList<ISymbolTable> importList)
        {
            /*try to read sth like this
            imports:[
                { name:"table1", version:1 },
                { name:"table2", version:12 }
            ]
            */
            reader.StepIn();

            IonType t;
            while ((t = reader.MoveNext()) != IonType.None)
            {
                if (reader.CurrentIsNull || t != IonType.Struct)
                    continue;
                var imported = FindImportedTable(reader, catalog);
                if (imported is null)
                    continue;
                importList.Add(imported);
            }

            reader.StepOut();
        }

        /// <summary>
        /// Read the table name and version and try to find it in the catalog.
        /// </summary>
        /// <returns>Null if no such table is found.</returns>
        private static ISymbolTable FindImportedTable(IIonReader reader, ICatalog catalog)
        {
            reader.StepIn();

            IonType t;
            string name = null;
            var version = -1;
            var maxId = -1;
            while ((t = reader.MoveNext()) != IonType.None)
            {
                if (reader.CurrentIsNull)
                    continue;

                var fieldId = reader.GetFieldNameSymbol().Sid;
                if (fieldId == SymbolToken.UnknownSid)
                {
                    fieldId = SystemSymbols.ResolveSidForSymbolTableField(reader.CurrentFieldName);
                }


                switch (fieldId)
                {
                    case SystemSymbols.NameSid:
                        if (t == IonType.String)
                        {
                            name = reader.StringValue();
                        }

                        break;
                    case SystemSymbols.VersionSid:
                        if (t == IonType.Int)
                        {
                            version = reader.IntValue();
                        }

                        break;
                    case SystemSymbols.MaxIdSid:
                        if (t == IonType.Int)
                        {
                            maxId = reader.IntValue();
                        }

                        break;
                }
            }

            reader.StepOut();

            if (string.IsNullOrWhiteSpace(name) || SystemSymbols.Ion.Equals(name))
                return null;

            if (version < 1)
            {
                version = 1;
            }

            var table = catalog?.GetTable(name, version);
            if (maxId < 0)
            {
                if (table == null || table.Version != version)
                    throw new IonException($@"Import of shared table {name}/{version} lacks a max_id field, but an exact match is 
                                                not found in the catalog");
                maxId = table.MaxId;
            }

            if (table == null)
            {
                throw new NotImplementedException("Substitute symbol table");
            }

            if (table.Version != version || table.MaxId != maxId)
            {
                throw new NotImplementedException("Substitute symbol table");
            }

            return table;
        }

        /// <summary>
        /// Try to read the symbols used in this datagram. The reader is in front of the $ion_symbol_table value.
        /// <para/> If the new symbol table inherits from the previous one, this new symtab should include all previous symbols.
        /// </summary>
        /// <param name="reader">Ion reader.</param>
        /// <param name="catalog">The catalog used to refer to tables that might be in the reader.</param>
        /// <param name="isOnStruct">Reader is before the $ion_symbol_table struct.</param>
        /// <returns>The Local symbol table.</returns>
        public static LocalSymbolTable Read(IIonReader reader, ICatalog catalog, bool isOnStruct)
        {
            var imports = ReadLocalSymbolTableImports(reader, catalog, isOnStruct, out var symbolList);
            return new LocalSymbolTable(imports, symbolList, true);
        }

        public ISymbolTable Copy() => new LocalSymbolTable(this, MaxId);
    }
}
