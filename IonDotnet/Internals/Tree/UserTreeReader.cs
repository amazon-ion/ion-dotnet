using IonDotnet.Tree;
using System;
using System.Collections.Generic;
using System.Text;

namespace IonDotnet.Internals.Tree
{
    internal class UserTreeReader : SystemTreeReader
    {
        /// The ID of system symbol {@value #ION_1_0}, as defined by Ion 1.0.
        public static readonly int ION_1_0_SID = 2;
        private readonly ICatalog _catalog;
        private ISymbolTable _currentSymtab;

        public UserTreeReader(IIonValue value, ICatalog catalog) : base(value)
        {
            _catalog = catalog;
            _currentSymtab = _systemSymbols;
        }

        public override ISymbolTable GetSymbolTable() => _currentSymtab;

        public override bool HasNext()
        {
            return NextHelperUser();
        }
        
        public override IonType MoveNext()
        {
            if (!NextHelperUser())
            {
                _current = null;
                return IonType.Null;
            }
            _current = _next;
            _next = null;
            return _current.Type();
        }

        bool NextHelperUser()
        {
            if (_eof) return false;
            if (_next != null) return true;

            ClearSystemValueStack();

            // read values from the system
            // reader and if they are system values
            // process them.  Return when we've
            // read all the immediate system values
            IonType next_type;
            while (true)
            {
                next_type = NextHelperSystem();

                if (_top == 0 && _parent.Type() == IonType.Datagram)
                {
                    if (IonType.Symbol == next_type)
                    {
                        var sym = _next;
                        if (sym.IsNull)
                        {
                            // there are no null values we will consume here
                            break;
                        }
                        int sid = sym.SymbolValue.Sid;
                        if (sid == -1) // if sid is unknown
                        {
                            String name = sym.SymbolValue.Text;
                            if (name != null)
                            {
                                sid = _systemSymbols.FindSymbolId(name);
                            }
                        }
                        if (sid == ION_1_0_SID && _next.GetTypeAnnotations().Count == 0)
                        {
                            // $ion_1_0 is read as an IVM only if it is not annotated
                            ISymbolTable symbols = _systemSymbols;
                            _currentSymtab = symbols;
                            PushSymbolTable(symbols);
                            _next = null;
                            continue;
                        }
                    }
                    else if (IonType.Struct == next_type && _next.HasAnnotation("$ion_symbol_table"))
                    {
                        // read a local symbol table
                        IIonReader reader = new UserTreeReader(_next, _catalog);
                        ISymbolTable symtab = ReaderLocalTable.ImportReaderTable(this, _catalog, true);
                        _currentSymtab = symtab;
                        PushSymbolTable(symtab);
                        _next = null;
                        continue;
                    }
                }
                // if we get here we didn't process a system
                // value, if we had we would have 'continue'd
                // so this is a value the user gets
                break;
            }
            return (next_type != IonType.Null);
        }

        private int _symbol_table_top = 0;
        private ISymbolTable[] _symbol_table_stack = new ISymbolTable[3]; // 3 is rare, IVM followed by a local sym tab with open content
        private void ClearSystemValueStack()
        {
            while (_symbol_table_top > 0)
            {
                _symbol_table_top--;
                _symbol_table_stack[_symbol_table_top] = null;
            }
        }
        private void PushSymbolTable(ISymbolTable symbols)
        {
            if (_symbol_table_top >= _symbol_table_stack.Length)
            {
                int new_len = _symbol_table_stack.Length * 2;
                ISymbolTable[] temp = new ISymbolTable[new_len];
                Array.Copy(_symbol_table_stack, 0, temp, 0, _symbol_table_stack.Length);
                _symbol_table_stack = temp;
            }
            _symbol_table_stack[_symbol_table_top++] = symbols;
        }
        private ISymbolTable PopPassedSymbolTable()
        {
            if (_symbol_table_top <= 0)
            {
                return null;
            }
            _symbol_table_top--;
            ISymbolTable symbols = _symbol_table_stack[_symbol_table_top];
            _symbol_table_stack[_symbol_table_top] = null;
            return symbols;
        }
    }
}
