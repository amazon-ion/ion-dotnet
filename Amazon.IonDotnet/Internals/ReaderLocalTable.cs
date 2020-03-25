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
using Amazon.IonDotnet.Internals.Binary;

namespace Amazon.IonDotnet.Internals
{
    /// <summary>
    /// This class is used for processing local symbol tables while reading ion data.
    /// </summary>
    internal class ReaderLocalTable : ISymbolTable
    {
        internal readonly List<ISymbolTable> Imports;
        private readonly List<string> _ownSymbols = new List<string>();
        private int _importedMaxId;

        internal ReaderLocalTable(ISymbolTable systemTable)
        {
            Debug.Assert(systemTable.IsSystem);
            Imports = new List<ISymbolTable> {systemTable};
        }

        /// <summary>
        /// Refresh the local symbol table to a valid state. Typically called after <see cref="Imports"/>
        /// and <see cref="_ownSymbols"/> has been mutated. 
        /// </summary>
        internal void Refresh()
        {
            var maxId = 0;
            foreach (var import in Imports)
            {
                Debug.Assert(import.IsShared);
                maxId += import.MaxId;
            }

            _importedMaxId = maxId;
        }

        public string Name => null;
        public int Version => 0;
        public bool IsLocal => true;
        public bool IsShared => false;
        public bool IsSubstitute => false;
        public bool IsSystem => false;
        public bool IsReadOnly => true;

        void ISymbolTable.MakeReadOnly() => throw new NotSupportedException();

        public ISymbolTable GetSystemTable() => Imports[0];

        public string IonVersionId => GetSystemTable().IonVersionId;

        public IReadOnlyList<ISymbolTable> GetImportedTables() => Imports;

        public int GetImportedMaxId() => _importedMaxId;

        public int MaxId => _importedMaxId + _ownSymbols.Count;

        SymbolToken ISymbolTable.Intern(string text) => throw new NotSupportedException();

        public SymbolToken Find(string text)
        {
            foreach (var import in Imports)
            {
                var t = import.Find(text);
                if (t != default)
                    return new SymbolToken(t.Text, SymbolToken.UnknownSid);
            }

            for (var i = 0; i < _ownSymbols.Count; i++)
            {
                if (_ownSymbols[i] == text)
                    return new SymbolToken(text, SymbolToken.UnknownSid);
            }

            return default;
        }

        public int FindSymbolId(string text)
        {
            var offset = 0;
            foreach (var import in Imports)
            {
                var t = import.FindSymbolId(text);
                if (t > 0)
                    return t + offset;
                offset += import.MaxId;
            }

            for (var i = 0; i < _ownSymbols.Count; i++)
            {
                if (_ownSymbols[i] == text)
                    return i + 1 + _importedMaxId;
            }

            return SymbolToken.UnknownSid;
        }

        public string FindKnownSymbol(int sid)
        {
            if (sid < SystemSymbols.IonSid || sid > MaxId)
                return null;

            if (sid > _importedMaxId)
                return _ownSymbols[sid - _importedMaxId - 1];

            var offset = 0;
            foreach (var import in Imports)
            {
                if (import.MaxId + offset >= sid)
                    return import.FindKnownSymbol(sid - offset);
                offset += import.MaxId;
            }

            // We should never get here.
            throw new IonException($"Sid={sid}");
        }

        public void WriteTo(IIonWriter writer) => writer.WriteValue(new SymbolTableReader(this));

        public IEnumerable<string> GetDeclaredSymbolNames() => _ownSymbols;

        public static ISymbolTable ImportReaderTable(IIonReader reader, ICatalog catalog, bool isOnStruct)
        {
            var table = reader.GetSymbolTable() as ReaderLocalTable ?? new ReaderLocalTable(reader.GetSymbolTable());
            var importList = table.Imports;
            var symbolList = table._ownSymbols;
            var oldLocalSymbolCount = symbolList.Count;

            if (!isOnStruct)
            {
                reader.MoveNext();
            }

            Debug.Assert(
                reader.CurrentType == IonType.Struct,
                "Invalid symbol table image passed in reader "
                    + reader.CurrentType
                    + " encountered when a struct was expected.");

            Debug.Assert(
                SystemSymbols.IonSymbolTable.Equals(reader.GetTypeAnnotations()[0]),
                "Local symbol tables must be annotated by "
                    + SystemSymbols.IonSymbolTable + ".");

            // Assume that we're standing before a struct.
            reader.StepIn();

            IonType fieldType;
            bool foundImport = false;
            bool foundLocals = false;
            while ((fieldType = reader.MoveNext()) != IonType.None)
            {
                if (reader.CurrentIsNull)
                {
                    continue;
                }

                var sid = reader.GetFieldNameSymbol().Sid;
                if (sid == SymbolToken.UnknownSid)
                {
                    // This is a user-defined IonReader or a pure DOM, fall
                    // back to text.
                    sid = SystemSymbols.ResolveSidForSymbolTableField(reader.CurrentFieldName);
                }

                switch (sid)
                {
                    case SystemSymbols.SymbolsSid:
                        // As per the spec, other field types are treated as
                        // empty lists.
                        if (foundLocals)
                        {
                            throw new IonException("Multiple symbol fields found within a single local symbol table.");
                        }

                        foundLocals = true;
                        if (fieldType == IonType.List)
                        {
                            ReadSymbolList(reader, symbolList);
                        }

                        break;

                    case SystemSymbols.ImportsSid:
                        if (foundImport)
                        {
                            throw new IonException("Multiple imports fields found within a single local symbol table.");
                        }

                        foundImport = true;
                        if (fieldType == IonType.List)
                        {
                            // List of symbol tables to imports.
                            ReadImportList(reader, catalog, importList);
                        }
                        // Trying to import the current table.
                        else if (fieldType == IonType.Symbol
                                 && reader.GetSymbolTable().IsLocal
                                 && (SystemSymbols.IonSymbolTable.Equals(reader.StringValue()) || reader.IntValue() == SystemSymbols.IonSymbolTableSid))
                                 
                        {
                            importList.Add(reader.GetSymbolTable());
                        }
                        else
                        {
                            throw new IonException("Invalid import format.");
                        }

                        break;
                    default:
                        // As per the spec, any other field is ignored.
                        break;
                }
            }

            reader.StepOut();

            if (!foundImport)
            {
                // No import field found, remove old symbols.
                symbolList.RemoveRange(0, oldLocalSymbolCount);
            }

            table.Refresh();
            return table;
        }

        private static void ReadSymbolList(IIonReader reader, ICollection<string> symbolList)
        {
            reader.StepIn();

            IonType type;
            while ((type = reader.MoveNext()) != IonType.None)
            {
                var text = type == IonType.String ? reader.StringValue() : null;

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
                // Cannot find table with that name, create an empty substitute symtab.
                table = new SubstituteSymbolTable(name, version, maxId);
            }

            if (table.Version != version || table.MaxId != maxId)
            {
                // A table with the name is found but version doesn't match.
                table = new SubstituteSymbolTable(table, version, maxId);
            }

            return table;
        }
    }
}
