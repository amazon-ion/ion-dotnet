namespace IonDotnet.Internals.Lite
{
    /// <summary>
    /// Context for child values of an IonDatagramLite. <br/>
    /// The datagram's child values that share the same local symbol table will share the same TopLevelContext.
    /// </summary>
    internal sealed class TopLevelContext : IContext
    {
        private readonly IonDatagramLite _datagram;
        private readonly ISymbolTable _symbolTable;

        public TopLevelContext(ISymbolTable symbolTable, IonDatagramLite datagram)
        {
            _datagram = datagram;
            _symbolTable = symbolTable;
        }

        public IonContainerLite GetContextContainer() => _datagram;

        public IonSystemLite GetSystem() => _datagram.GetSystem();

        public ISymbolTable GetContextSymbolTable() => _symbolTable;
    }
}
