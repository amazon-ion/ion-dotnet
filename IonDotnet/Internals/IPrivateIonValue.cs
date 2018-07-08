namespace IonDotnet.Internals
{
    internal interface IPrivateIonValue : IIonValue
    {
        /// <summary>
        /// The offset of this value in its containers member list
        /// </summary>
        int ElementId { get; }

        /// <summary>
        /// Overrides <see cref="IIonValue.FieldNameSymbol"/> for use when there exists a
        /// SymbolTableProvider implementation for this IonValue.
        /// </summary>
        /// <param name="symbolTableProvider">SymbolTableProvider</param>
        /// <returns>Token of this symbol on the table</returns>
        SymbolToken GetFieldNameSymbol(ISymbolTableProvider symbolTableProvider);

        /// <summary>
        /// Include a setter for the current SymbolTable of this value
        /// </summary>
        /// <remarks>
        /// This may directly apply to this IonValue if this value is either loose or a top level datagram member.
        /// Or it may be delegated to the IonContainer this value is a contained in. <br/>
        /// Assigning null forces any symbol values to be resolved to strings and any associated symbol
        /// table will be removed.
        /// </remarks>
        new ISymbolTable SymbolTable { get; set; }
        
        /// <returns>
        /// Symbol table that is directly associated with this value, without doing any recursive lookup
        /// </returns>
        /// <remarks>
        /// Values that are not top-level will return null as they don't actually own their own symbol table.
        /// </remarks>
        ISymbolTable GetAssignedSymbolTable();
        
        /// <summary>
        /// Overrides <see cref="IIonValue.GetTypeAnnotationSymbols"/> for use when there exists a
        /// SymbolTableProvider implementation for this IonValue.
        /// </summary>
        /// <param name="symbolTableProvider">provides this IonValue's symbol table</param>
        /// <returns>the type annotation SymbolTokens</returns>
        SymbolToken[] GetTypeAnnotationSymbols(ISymbolTableProvider symbolTableProvider);
    }
}
