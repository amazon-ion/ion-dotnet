using System.Diagnostics;
using IonDotnet.Systems;

namespace IonDotnet.Utils
{
    public static class Symbols
    {
        public static readonly ISymbolTable[] EmptySymbolTablesArray = new ISymbolTable[0];


        public static readonly SymbolToken[] SystemSymbolTokens =
        {
            new SymbolToken(SystemSymbols.Ion, SystemSymbols.IonSid),
            new SymbolToken(SystemSymbols.Ion10, SystemSymbols.Ion10Sid),
            new SymbolToken(SystemSymbols.IonSymbolTable, SystemSymbols.IonSymbolTableSid),
            new SymbolToken(SystemSymbols.Name, SystemSymbols.NameSid),
            new SymbolToken(SystemSymbols.Version, SystemSymbols.VersionSid),
            new SymbolToken(SystemSymbols.Imports, SystemSymbols.ImportsSid),
            new SymbolToken(SystemSymbols.Symbols, SystemSymbols.SymbolsSid),
            new SymbolToken(SystemSymbols.MaxId, SystemSymbols.MaxIdSid),
            new SymbolToken(SystemSymbols.IonSharedSymbolTable, SystemSymbols.IonSharedSymbolTableSid)
        };

        public static SymbolToken GetSystemSymbol(int sid) => SystemSymbolTokens[sid - 1];

        /// <summary>
        /// Try to re-make the token in the context of the <paramref name="table"/>.
        /// </summary>
        /// <param name="table">Symbol table</param>
        /// <param name="token">Un-localized token</param>
        /// <returns>Localized token</returns>
        public static SymbolToken Localize(ISymbolTable table, SymbolToken token)
        {
            var newToken = token;
            //try to localize
            if (token.Text == null)
            {
                var text = table.FindKnownSymbol(token.Sid);
                if (text != null)
                {
                    newToken = new SymbolToken(text, token.Sid);
                }
            }
            else
            {
                newToken = table.Find(token.Text);
                if (newToken == default)
                {
                    newToken = new SymbolToken(token.Text, SymbolToken.UnknownSid);
                }
            }

            return newToken;
        }

        /// <summary>
        /// Returns a catalog that contains all shared symbol tables that the reader has read so far.
        /// </summary> 
        /// <param name="readerTable">The reader's local symbol table. Typically obtained by calling <see cref="IIonReader.GetSymbolTable"/>.</param>
        /// <remarks>
        /// Normally when a text or binary Ion data with shared symbol tables is read, the materialized object (such as 
        /// a .Net POCO object or an <see cref="IonDotnet.Tree.IonDatagram"/> does not have the reference to these tables.
        /// As such, systems that want to reuse those shared tables should extract them after reading through all the values.
        /// This method provides a shortcut to do get a catalog that contains those tables.
        /// </remarks>
        public static ICatalog GetReaderCatalog(ISymbolTable readerTable)
        {
            var catalog = new SimpleCatalog();
            foreach (var importedTable in readerTable.GetImportedTables())
            {
                Debug.Assert(importedTable.IsShared);
                if (importedTable.IsSystem || importedTable.IsSubstitute)
                {
                    continue;
                }

                catalog.PutTable(importedTable);
            }

            return catalog;
        }

        /// <summary>
        /// Write the name/version/maxid of an imported table.
        /// </summary>
        /// <param name="writer">Current writer.</param>
        /// <param name="importedTable">Imported table.</param>
        /// <remarks>This assumes the writer is in the import list.</remarks>
        internal static void WriteImportTable(this IIonWriter writer, ISymbolTable importedTable)
        {
            writer.StepIn(IonType.Struct); // {name:'a', version: 1, max_id: 33}
            writer.SetFieldNameSymbol(GetSystemSymbol(SystemSymbols.NameSid));
            writer.WriteString(importedTable.Name);
            writer.SetFieldNameSymbol(GetSystemSymbol(SystemSymbols.VersionSid));
            writer.WriteInt(importedTable.Version);
            writer.SetFieldNameSymbol(GetSystemSymbol(SystemSymbols.MaxIdSid));
            writer.WriteInt(importedTable.MaxId);
            writer.StepOut();
        }
    }
}
