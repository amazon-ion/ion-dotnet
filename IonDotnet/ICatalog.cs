namespace IonDotnet
{
    // juste test ci
    /// <summary>
    /// Collects shared symbol tables for use by an <see cref="IIonSystem"/>
    /// </summary>
    public interface ICatalog
    {
        /// <summary>
        /// Gets a symbol table with a specific name 
        /// </summary>
        /// <param name="name">Table name</param>
        /// <returns>The highest version possible.</returns>
        ISymbolTable GetTable(string name);

        /// <summary>
        /// Gets a symbol table with a specific name and version
        /// </summary>
        /// <param name="name">Table name</param>
        /// <param name="version">Table version</param>
        /// <returns>Exact match if possible, otherwise best effort</returns>
        ISymbolTable GetTable(string name, int version);
    }
}
