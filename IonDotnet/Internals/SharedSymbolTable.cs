using System;
using System.Collections.Generic;
using IonDotnet.Internals.Binary;

namespace IonDotnet.Internals
{
    /// <inheritdoc />
    /// <summary>
    /// An immutable, thread-safe shared symbol table. Used for system table
    /// </summary>
    internal sealed class SharedSymbolTable : ISymbolTable
    {
        public static readonly ISymbolTable[] EmptyArray = new ISymbolTable[0];

        private static readonly string[] SystemSymbolsArray =
        {
            SystemSymbols.Ion,
            SystemSymbols.Ion10,
            SystemSymbols.IonSymbolTable,
            SystemSymbols.Name,
            SystemSymbols.Version,
            SystemSymbols.Imports,
            SystemSymbols.Symbols,
            SystemSymbols.MaxId,
            SystemSymbols.IonSharedSymbolTable
        };

        private static readonly ISymbolTable Ion10SystemSymtab;

        static SharedSymbolTable()
        {
            var map = new Dictionary<string, int>();
            for (int i = 0, l = SystemSymbolsArray.Length; i < l; i++)
            {
                map[SystemSymbolsArray[i]] = i + 1;
            }

            Ion10SystemSymtab = new SharedSymbolTable(SystemSymbols.Ion, 1, SystemSymbolsArray, map);
        }

        private readonly IDictionary<string, int> _symbolsMap;
        private readonly string[] _symbolNames;

        private SharedSymbolTable(string name, int version, List<string> symbolNames, IDictionary<string, int> symbolsMap)
            : this(name, version, symbolNames.ToArray(), symbolsMap)
        {
        }

        private SharedSymbolTable(string name, int version, string[] symbolNames, IDictionary<string, int> symbolsMap)
        {
            Name = name;
            Version = version;
            _symbolsMap = symbolsMap;
            _symbolNames = symbolNames;
        }

        public string Name { get; }
        public int Version { get; }
        public bool IsLocal { get; } = false;
        public bool IsShared { get; } = true;
        public bool IsSubstitute { get; } = false;
        public bool IsSystem => Name == SystemSymbols.Ion;
        public bool IsReadOnly { get; } = true;

        public void MakeReadOnly()
        {
            //already is
        }

        public ISymbolTable GetSystemTable() => IsSystem ? this : null;

        public string IonVersionId
        {
            get
            {
                if (!IsSystem)
                    return null;
                if (Version != 1)
                    throw new IonException($"Unrecognized version {Version}");
                return SystemSymbols.Ion10;
            }
        }

        IReadOnlyList<ISymbolTable> ISymbolTable.GetImportedTables()
        {
            return EmptyArray;
        }

        public int GetImportedMaxId() => 0;

        public int MaxId => _symbolNames.Length;

        public SymbolToken Intern(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            var symtok = Find(text);
            if (symtok == default)
                throw new InvalidOperationException("Table is read-only");
            return symtok;
        }

        public SymbolToken Find(string text)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            if (!_symbolsMap.TryGetValue(text, out var sid))
                return default;

            var internedText = _symbolNames[sid - 1];
            // Normalize the SID and import reference to this table.
            return new SymbolToken(internedText, sid, new ImportLocation(Name, sid));
        }

        public int FindSymbolId(string name)
            => _symbolsMap.TryGetValue(name, out var sid) ? sid : SymbolToken.UnknownSid;

        public string FindKnownSymbol(int sid)
        {
            if (sid < 0)
                return null;
            var offset = sid - 1;
            return sid != 0 && offset < _symbolNames.Length ? _symbolNames[offset] : null;
        }

        public void WriteTo(IIonWriter writer)
        {
            writer.WriteValues(new SymbolTableReader(this));
        }

        public IEnumerable<string> GetDeclaredSymbolNames() => _symbolNames;

        /// <summary>
        /// Get the system symbol table
        /// </summary>
        /// <param name="version">Ion version</param>
        /// <returns>System symbol table</returns>
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
        internal static ISymbolTable GetSystem(int version)
        {
            if (version != 1)
                throw new ArgumentException("only Ion 1.0 system symbols are supported");
            return Ion10SystemSymtab;
        }

        internal static ISymbolTable NewSharedSymbolTable(string name, int version, ISymbolTable priorSymtab, IEnumerable<string> symbols)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name), "Must not be empty");
            if (symbols == null)
                throw new ArgumentNullException(nameof(symbols), "Must not be null");
            if (version < 1)
                throw new ArgumentException("Must be at least 1", nameof(version));

            var (symbolList, symbolMap) = PrepSymbolListAndMap(priorSymtab, symbols);
            return new SharedSymbolTable(name, version, symbolList, symbolMap);
        }

        private static (List<string> symbolList, Dictionary<string, int> symbolMap) PrepSymbolListAndMap(
            ISymbolTable priorSymtab, IEnumerable<string> symbols)
        {
            var sid = 1;
            var symbolList = new List<string>();
            var symbolMap = new Dictionary<string, int>();
            if (priorSymtab != null)
            {
                var priorSymbols = priorSymtab.GetDeclaredSymbolNames();
                foreach (var text in priorSymbols)
                {
                    if (text != null && !symbolMap.ContainsKey(text))
                    {
                        symbolMap[text] = sid;
                    }

                    symbolList.Add(text);
                    sid++;
                }
            }

            foreach (var symbol in symbols)
            {
                if (symbolMap.ContainsKey(symbol))
                    continue;
                symbolMap[symbol] = sid;
                symbolList.Add(symbol);
                sid++;
            }

            return (symbolList, symbolMap);
        }
    }
}
