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

namespace Amazon.IonDotnet.Internals
{
    using System;
    using System.Collections.Generic;
    using Amazon.IonDotnet.Internals.Binary;

    /// <inheritdoc />
    /// <summary>
    /// An immutable, thread-safe shared symbol table. Used for system table.
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
            SystemSymbols.IonSharedSymbolTable,
        };

        private static readonly ISymbolTable Ion10SystemSymtab;

        private readonly IDictionary<string, int> symbolsMap;
        private readonly string[] symbolNames;

        static SharedSymbolTable()
        {
            var map = new Dictionary<string, int>();
            for (int i = 0, l = SystemSymbolsArray.Length; i < l; i++)
            {
                map[SystemSymbolsArray[i]] = i + 1;
            }

            Ion10SystemSymtab = new SharedSymbolTable(SystemSymbols.Ion, 1, SystemSymbolsArray, map);
        }

        private SharedSymbolTable(string name, int version, List<string> symbolNames, IDictionary<string, int> symbolsMap)
            : this(name, version, symbolNames.ToArray(), symbolsMap)
        {
        }

        private SharedSymbolTable(string name, int version, string[] symbolNames, IDictionary<string, int> symbolsMap)
        {
            this.Name = name;
            this.Version = version;
            this.symbolsMap = symbolsMap;
            this.symbolNames = symbolNames;
        }

        public string Name { get; }

        public int Version { get; }

        public bool IsLocal { get; } = false;

        public bool IsShared { get; } = true;

        public bool IsSubstitute { get; } = false;

        public bool IsSystem => this.Name == SystemSymbols.Ion;

        public bool IsReadOnly { get; } = true;

        public string IonVersionId
        {
            get
            {
                if (!this.IsSystem)
                {
                    return null;
                }

                if (this.Version != 1)
                {
                    throw new IonException($"Unrecognized version {this.Version}");
                }

                return SystemSymbols.Ion10;
            }
        }

        public int MaxId => this.symbolNames.Length;

        public void MakeReadOnly()
        {
            // IsReadOnly is always true
        }

        public ISymbolTable GetSystemTable() => this.IsSystem ? this : null;

        IReadOnlyList<ISymbolTable> ISymbolTable.GetImportedTables()
        {
            return EmptyArray;
        }

        public int GetImportedMaxId() => 0;

        public SymbolToken Intern(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var symtok = this.Find(text);
            if (symtok == default)
            {
                throw new InvalidOperationException("Table is read-only");
            }

            return symtok;
        }

        public SymbolToken Find(string text)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (!this.symbolsMap.TryGetValue(text, out var sid))
            {
                return default;
            }

            var internedText = this.symbolNames[sid - 1];
            return new SymbolToken(internedText, SymbolToken.UnknownSid);
        }

        public int FindSymbolId(string name)
            => this.symbolsMap.TryGetValue(name, out var sid) ? sid : SymbolToken.UnknownSid;

        public string FindKnownSymbol(int sid)
        {
            if (sid < 0)
            {
                return null;
            }

            var offset = sid - 1;
            return sid != 0 && offset < this.symbolNames.Length ? this.symbolNames[offset] : null;
        }

        public void WriteTo(IIonWriter writer)
        {
            writer.WriteValues(new SymbolTableReader(this));
        }

        public IEnumerable<string> GetDeclaredSymbolNames() => this.symbolNames;

        /// <summary>
        /// Get the system symbol table.
        /// </summary>
        /// <param name="version">Ion version.</param>
        /// <returns>System symbol table.</returns>
        internal static ISymbolTable GetSystem(int version)
        {
            if (version != 1)
            {
                throw new ArgumentException("only Ion 1.0 system symbols are supported");
            }

            return Ion10SystemSymtab;
        }

        internal static ISymbolTable NewSharedSymbolTable(string name, int version, ISymbolTable priorSymtab, IEnumerable<string> symbols)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name), "Must not be empty");
            }

            if (symbols == null)
            {
                throw new ArgumentNullException(nameof(symbols), "Must not be null");
            }

            if (version < 1)
            {
                throw new ArgumentException("Must be at least 1", nameof(version));
            }

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
                {
                    continue;
                }

                symbolMap[symbol] = sid;
                symbolList.Add(symbol);
                sid++;
            }

            return (symbolList, symbolMap);
        }
    }
}
