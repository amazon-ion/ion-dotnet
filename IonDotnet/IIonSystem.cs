using System;
using System.Collections.Generic;
using System.IO;

namespace IonDotnet
{
    /// <summary>
    /// Entry point to all things Ion
    /// </summary>
    public interface IIonSystem
    {
        /// <summary>
        /// Get the default system table
        /// </summary>
        /// <returns></returns>
        ISymbolTable GetSystemSymbolTable();

        /// <summary>
        /// Get the system table for an ion version
        /// </summary>
        /// <param name="ionVersionId">Ion version</param>
        /// <returns>System table</returns>
        ISymbolTable GetSystemSymbolTable(string ionVersionId);

        /// <summary>
        /// Catalog used by this system
        /// </summary>
        ICatalog Catalog { get; }

        /// <summary>
        /// Creates a new local symbol table based on specific imported tables
        /// </summary>
        /// <param name="imports">the set of shared symbol tables to import. The first (and only the first) may be a system table.</param>
        /// <returns>a new local symbol table</returns>
        ISymbolTable NewLocalSymbolTable(params ISymbolTable[] imports);

        /// <summary>
        /// Creates a new shared symbol table containing a given set of symbols. The table will contain symbols from the
        /// previous versions and <see cref="imports"/> and <see cref="newSymbols"/>
        /// </summary>
        /// <param name="name">Table name</param>
        /// <param name="version">Version</param>
        /// <param name="newSymbols">New symbols</param>
        /// <param name="imports">Non-system tables to import</param>
        /// <returns></returns>
        ISymbolTable NewSharedSymbolTable(string name, int version, IEnumerable<string> newSymbols, params ISymbolTable[] imports);

        /// <summary>
        /// Materializes a shared symbol table from its serialized form.
        /// </summary>
        /// <param name="reader">Ion reader</param>
        /// <returns>Symbol table</returns>
        /// <remarks>
        /// This method expects the reader to be positioned before the struct.
        /// The reader's next() method has not been called to position the reader on the symbol table struct.
        /// </remarks>
        ISymbolTable NewSharedSymbolTable(IIonReader reader);

        /// <summary>
        /// Constructs a new loader instance using the default catalog
        /// </summary>
        /// <returns>New loader</returns>
        ILoader NewLoader();

        /// <summary>
        /// Constructs a new loader instance using the specified catalog
        /// </summary>
        /// <param name="catalog">Catalog</param>
        /// <returns>New loader</returns>
        ILoader NewLoader(ICatalog catalog);

        /// <summary>
        /// Gets the default system loader.
        /// </summary>
        /// <returns>Loader</returns>
        /// <remarks>Applications may replace this loader with one configured appropriately, and then access it here.</remarks>
        ILoader GetLoader();

        IEnumerable<IIonValue> Enumerate(TextReader textReader);

        IEnumerable<IIonValue> Enumerate(Stream stream);

        IEnumerable<IIonValue> Enumerate(byte[] ionData);

        IIonValue SingleValue(string ionText);

        IIonValue SingleValue(byte[] ionData);

        IIonReader NewReader(string ionText);

        IIonReader NewReader(byte[] ionData);

        IIonReader NewReader(ArraySegment<byte> ionData);

        IIonReader NewReader(Stream ionData);

        IIonReader NewReader(TextReader ionText);

        IIonReader NewReader(IIonValue value);

        /// <summary>
        /// Creates a new writer that will add values to the given container.
        /// </summary>
        /// <param name="container">Container</param>
        /// <returns>New writer</returns>
        IIonWriter NewWriter(IIonContainer container);

        IIonWriter NewTextWriter(Stream outputStream, params ISymbolTable[] imports);

        IIonWriter NewBinaryWriter(Stream outputStream, params ISymbolTable[] imports);

        IIonDatagram NewDatagram();

        IIonDatagram NewDatagram(IIonValue initialChild);

        IIonDatagram NewDatagram(params ISymbolTable[] imports);

        IIonValue NewValue(IIonReader reader);
    }
}
