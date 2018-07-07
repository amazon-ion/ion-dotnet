namespace IonDotnet.Internals.Lite
{
    /// <summary>
    /// Provides the parent, system and symbol table definitions that are shared by one or more hierarchies of IonValues
    /// </summary>
    internal interface IContext
    {
        IonContainerLite GetContextContainer();

        IonSystemLite GetSystem();

        ISymbolTable GetContextSymbolTable();
    }
}
