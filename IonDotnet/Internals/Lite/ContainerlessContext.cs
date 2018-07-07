namespace IonDotnet.Internals.Lite
{
    internal class ContainerlessContext : IContext
    {
        private readonly IonSystemLite _system;
        private readonly ISymbolTable _symbols;

        public ContainerlessContext(IonSystemLite system, ISymbolTable symbols = null)
        {
            _system = system;
            _symbols = symbols;
        }

        public IonContainerLite GetContextContainer() => null;

        public IonSystemLite GetSystem() => _system;

        public ISymbolTable GetContextSymbolTable() => _symbols;
    }
}
