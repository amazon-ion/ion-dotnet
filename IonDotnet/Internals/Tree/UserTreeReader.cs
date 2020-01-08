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

        public bool hasNext()
        {
            return next_helper_user();
        }
        
        public IonType MoveNext()
        {
            if (!next_helper_user())
            {
                _current = null;
                return null;
            }
            _current = _next;
            _next = null;
            return _current.Type();
        }
    }
}
