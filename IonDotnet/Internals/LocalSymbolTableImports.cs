using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace IonDotnet.Internals
{
    /// <summary>
    /// Manages the tables imported to a local symbol table
    /// </summary>
    internal sealed class LocalSymbolTableImports
    {
        private readonly ISymbolTable[] _imports;
        public int MaxId { get; }

        public LocalSymbolTableImports(IList<ISymbolTable> imports)
        {
            _imports = new ISymbolTable[imports.Count];
            for (var i = 0; i < imports.Count; i++)
            {
                var symtab = imports[i];
                if (symtab.IsLocal)
                {
                    //TODO handle local imports
                    throw new NotImplementedException();
                }

                _imports[i] = symtab;
            }

            // TODO fix this asap
            MaxId = _imports[0].MaxId;
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
            //TODO implements this properly
            return SymbolToken.None;
        }

        public int FindSymbol(string text)
        {
            //TODO implements this properly
            return SymbolToken.UnknownSid;
        }

        public string FindKnownSymbol(int sid)
        {
            //TODO implements this properly
            return string.Empty;
        }
    }
}
