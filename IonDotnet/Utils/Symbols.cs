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
        /// Try to re-make the token in the context of the <paramref name="table"/>
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
    }
}
