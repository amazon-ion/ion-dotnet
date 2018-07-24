using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;

namespace IonDotnet.Internals
{
    /// <summary>
    /// Manages the tables imported to a local symbol table
    /// </summary>
    internal sealed class LocalSymbolTableImports
    {
        private readonly ISymbolTable[] _imports;
        private readonly int[] _baseIds;

        public int MaxId { get; }

        public LocalSymbolTableImports(ISymbolTable systemTable, IList<ISymbolTable> imports)
        {
            if (!systemTable.IsSystem) throw new ArgumentException("Not a system table", nameof(systemTable));

            var importCounts = imports?.Count ?? 0;
            _imports = new ISymbolTable[importCounts + 1];
            _imports[0] = systemTable;
            var startIdx = _imports?[0]?.IsSystem == true ? 1 : 0;


            if (imports != null)
            {
                var ii = 1;
                for (var i = startIdx; i < importCounts; i++, ii++)
                {
                    var symtab = imports[i];
                    if (symtab.IsSystem) throw new IonException("System table cannot be imported");

                    if (symtab is LocalSymbolTable localSymtab)
                    {
                        _imports[ii] = localSymtab.IsReadOnly ? localSymtab : localSymtab.Copy();
                    }
                    else
                    {
                        _imports[ii] = symtab;
                    }
                }
            }

            _baseIds = new int[_imports.Length];
            MaxId = PrepBaseSids(_baseIds, _imports);
        }

        private static int PrepBaseSids(int[] baseSids, IReadOnlyList<ISymbolTable> imports)
        {
            Debug.Assert(imports != null && imports.Count > 0);
            Debug.Assert(imports[0].IsSystem);

            baseSids[0] = 0;
            var total = imports[0].MaxId;
            for (var i = 1; i < imports.Count; i++)
            {
                baseSids[i] = total;
                total += imports[i].MaxId;
            }

            return total;
        }

        /// <summary>
        /// Return the system table of this list (should be [0])
        /// </summary>
        public ISymbolTable SystemTable
        {
            get
            {
                Debug.Assert(_imports[0].IsSystem);
                return _imports[0];
            }
        }

        /// <returns>Enumerable of non-system tables</returns>
        public IEnumerable<ISymbolTable> GetSymbolTables()
        {
            for (var i = 1; i < _imports.Length; i++)
            {
                yield return _imports[i];
            }
        }

        /// <summary>
        /// Find the symbol text
        /// </summary>
        /// <param name="text">Symbol's text</param>
        /// <returns>The token</returns>
        public SymbolToken Find(string text)
        {
            for (var i = 0; i < _imports.Length; i++)
            {
                var token = _imports[i].Find(text);
                if (token == default) continue;

                return new SymbolToken(text, token.Sid + _baseIds[i]);
            }

            return SymbolToken.None;
        }

        public int FindSymbol(string text) => Find(text).Sid;

        public string FindKnownSymbol(int sid)
        {
            if (sid > MaxId) return null;

            int i, prevBaseSid = 0;
            for (i = 0; i < _imports.Length; i++)
            {
                var baseSid = _baseIds[i];
                if (sid <= baseSid) break;
                prevBaseSid = baseSid;
            }

            var importScopedSid = sid - prevBaseSid;
            return _imports[i - 1].FindKnownSymbol(importScopedSid);
        }
    }
}
