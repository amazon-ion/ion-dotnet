namespace IonDotnet.Systems
{
    public interface IMutableCatalog : ICatalog
    {
        /// <summary>
        /// Adds a symbol table to this catalog. 
        /// </summary>
        /// <param name="sharedTable">Shared, non-system, non-subtitute table</param>
        void PutTable(ISymbolTable sharedTable);
    }
}
