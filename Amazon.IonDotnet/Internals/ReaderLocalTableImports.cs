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
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Amazon.IonDotnet.Internals
{
    /**
     * This class manages the system symbol table and any shared symbol table(s)
     * imported by a local symbol table. It provides "find" methods to find
     * either symbol Ids or names in the imported tables.
     * <p>
     * This class is <b>immutable</b>, and hence safe for use by multiple threads.
     */
    // TODO amzn/ion-java#37 Create specialized class to handle the common case where
    //      there are zero or one imported non-system shared symtab(s).
    public sealed class ReaderLocalTableImports
    {
        /**
         * The maxId of all imported tables, i.e., the sum of all maxIds declared
         * by symtabs in {@link #myImports}.
         */
        public int MaxId { get; }

        /**
         * The symtabs imported by a local symtab, never null or empty. The first
         * symtab must be a system symtab, the rest must be non-system shared
         * symtabs.
         */
        private readonly ISymbolTable[] imports;

        /**
         * The base Sid of each symtab in {@link #imports} in parallel, e.g.,
         * {@link #baseSids}[0] references {@link #imports}[0]. Must be
         * the same length as {@link #imports}.
         */
        private readonly int[] baseSids;

        /**
         * Constructor, takes the passed-in {@code importTables} containing the
         * imported symtabs.
         *
         * @param importTables
         *          The imported symtabs, must contain at least one element; the
         *          first element must be a system symtab, the rest must be
         *          non-system shared symtabs.
         *
         * @throws ArgumentException
         *          If any import is a local table, or if any but the first is a
         *          system table.
         * @throws NullPointerException
         *          If any import is null.
         */
        public ReaderLocalTableImports(List<ISymbolTable> importTables)
        {
            ValidateImports(importTables);

            var importTablesSize = importTables.Count;

            // Detects and adapts local tables so they are importable.
            imports = new ISymbolTable[importTablesSize];
            for (var i = 0; i < importTables.Count; i++)
            {
                ISymbolTable symbolTable = importTables[i];
                if (symbolTable.IsLocal)
                {
                    // TODO-BQ: What does this adapter do?
                    // imports[i] = LocalSymbolTableImportAdapter.of((LocalSymbolTable)symbolTable);
                }
                else
                {
                    imports[i] = symbolTable;
                }
            }

            baseSids = new int[importTablesSize];
            MaxId = PrepBaseSids(baseSids, imports);
        }

        public string FindKnownSymbol(int sid)
        {
            string name = null;

            if (sid <= MaxId)
            {
                var previousBaseSid = 0;

                int i;
                for (i = 1; i < imports.Length; i++)
                {
                    var baseSid = baseSids[i];
                    if (sid <= baseSid)
                    {
                        break;
                    }

                    previousBaseSid = baseSid;
                }

                // If we run over imports.Length, the sid is in the last symtab.
                var importScopedSid = sid - previousBaseSid;
                name = imports[i - 1].FindKnownSymbol(importScopedSid);
            }

            return name;
        }

        public int FindSymbol(string name)
        {
            SymbolToken tok = Find(name);
            return tok == null ? SymbolToken.UnknownSid : tok.Sid;
        }

        /**
         * Finds a symbol already interned by an import, returning the lowest
         * known SID.
         * <p>
         * This method will not necessarily return the same instance given the
         * same input.
         *
         * @param text The symbol text to find.
         *
         * @return
         *          The interned symbol (with both text and SID), or {@code null}
         *          if it's not defined by an imported table.
         */
        public SymbolToken Find(string text)
        {
            for (var i = 0; i < imports.Length; i++)
            {
                ISymbolTable importedTable = imports[i];
                SymbolToken tok = importedTable.Find(text);

                if (tok != null)
                {
                    var sid = tok.Sid + baseSids[i];
                    text = tok.Text;

                    Debug.Assert(text != null);

                    return new SymbolToken(text, sid);
                }
            }

            return SymbolToken.None;
        }

        /**
         * Gets the sole system symtab.
         */
        public ISymbolTable GetSystemSymbolTable()
        {
            Debug.Assert(imports[0].IsSystem);

            return imports[0];
        }

        /**
         * Gets all non-system shared symtabs (if any).
         *
         * @return A newly allocated copy of the imported symtabs.
         */
        public ISymbolTable[] GetImportedTables()
        {
            var count = imports.Length - 1; // We don't include system symtab.
            ISymbolTable[] symTblImports = new ISymbolTable[count];
            if (count > 0)
            {
                // Defensive copy.
                Array.Copy(imports, 1, symTblImports, 0, count);
            }

            return symTblImports;
        }

        /**
         * Returns the {@link #imports} member field without making a copy.
         * <p>
         * <b>Note:</b> Callers must not modify the resulting SymbolTable array!
         * This will violate the immutability property of this class.
         *
         * @return
         *          The backing array of imported symtabs, as-is; the first element
         *          is a system symtab, the rest are non-system shared symtabs.
         *
         * @see #GetImportedTables()
         */
        public ISymbolTable[] GetImportedTablesNoCopy()
        {
            return imports;
        }

        // TODO-BQ: Do we need a ToString() method here?

        /**
         * Determines whether the passed-in instance has the same sequence of
         * symbol table imports as this instance. Note that equality of these
         * imports are checked using their reference, instead of their semantic
         * state.
         */
        public bool EqualImports(ReaderLocalTableImports other)
        {
            return ReferenceEquals(imports, other.imports);
        }

        /**
         * Collects the necessary maxId info from the passed-in {@code imports}
         * and populates the {@code baseSids} array.
         *
         * @return the sum of all imports' maxIds
         *
         * @throws ArgumentException
         *          if any symtab beyond the first is a local or system symtab
         */
        private static int PrepBaseSids(int[] baseSids, ISymbolTable[] imports)
        {
            ISymbolTable firstImport = imports[0];

            Debug.Assert(
                firstImport.IsSystem,
                "First symtab must be a system symtab.");

            baseSids[0] = 0;
            var total = firstImport.MaxId;

            for (var i = 1; i < imports.Length; i++)
            {
                ISymbolTable importedTable = imports[i];

                if (importedTable.IsSystem)
                {
                    throw new ArgumentException("Only non-system shared tables can be imported.");
                }

                baseSids[i] = total;
                total += imports[i].MaxId;
            }

            return total;
        }

        /**
         * Validates the import list to ensure that if there is a {@link LocalSymbolTable} in it then it's a single import
         * apart from the system table.
         */
        private void ValidateImports(List<ISymbolTable> importTables)
        {
            var sizeWithoutSystemTables = importTables.Count;
            var numberOfLocalTables = 0;

            foreach (ISymbolTable table in importTables)
            {
                if (table.IsLocal)
                {
                    numberOfLocalTables++;
                }

                if (table.IsSystem)
                {
                    sizeWithoutSystemTables--;
                }
            }

            if (numberOfLocalTables > 0 && sizeWithoutSystemTables != 1)
            {
                throw new ArgumentException("When importing LocalSymbolTables it needs to be the only import.");
            }
        }
    }
}
