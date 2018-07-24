using System.Collections.Generic;

namespace IonDotnet.Utils
{
    public static class Symbols
    {
        public static readonly IReadOnlyList<SymbolToken> SystemSymbolTokens = new[]
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
    }
}
