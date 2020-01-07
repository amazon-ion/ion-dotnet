using IonDotnet.Tree;
using System;
using System.Collections.Generic;
using System.Text;

namespace IonDotnet.Internals.Tree
{
    internal class UserTreeReader : SystemTreeReader
    {
        private ISymbolTable _currentSymtab;
        private readonly ICatalog _catalog;

        public UserTreeReader(IIonValue value, ICatalog catalog) : base(value)
        {
            _catalog = catalog;
            _currentSymtab = _systemSymbols;
        }

        public override ISymbolTable GetSymbolTable() => _currentSymtab;
    }
}
