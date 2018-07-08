namespace IonDotnet.Internals
{
    internal interface IPrivateDatagram : IPrivateIonValue, IIonDatagram
    {
        /// <summary>
        /// TODO figure out what this does
        /// </summary>
        void AppendTrailingSymbolTable(ISymbolTable symtab);
    }
}
