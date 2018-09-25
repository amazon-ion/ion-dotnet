namespace IonDotnet
{
    /// <summary>
    /// A collection of shared symbol tables.
    /// </summary>
    public interface ICatalog
    {
        /// <summary>
        /// Gets the symbol table with a specific name with the latest version.
        /// </summary>
        /// <param name="name">Table name</param>
        /// <returns>The highest version possible.</returns>
        ISymbolTable GetTable(string name);

        /// <summary>
        /// Gets a symbol table with a specific name and version.
        /// </summary>
        /// <param name="name">Table name</param>
        /// <param name="version">Table version</param>
        /// <returns>Exact match if possible, otherwise best effort.</returns>
        ISymbolTable GetTable(string name, int version);
    }
}
