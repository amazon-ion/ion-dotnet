using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace IonDotnet.Tree
{
    /// <summary>
    /// An ion datagram is a special kind of value which represents a stream of Ion values.
    /// </summary>
    public sealed class IonDatagram : IonSequence
    {
        private readonly List<ISymbolTable> _symbolTables = new List<ISymbolTable>();

        public IonDatagram() : base(false)
        {
        }

        public override IonType Type => IonType.Datagram;

        public override IonValue Container
        {
            get => null;
            internal set => throw new InvalidOperationException("Cannot set the container of an Ion Datagram");
        }

        public override ISymbolTable GetSymbolTable()
        {
            return _symbolTables.Count == 0 ? null : _symbolTables[_symbolTables.Count - 1];
        }

        /// <summary>
        /// Adding an item to the datagram will mark the current symbol table.
        /// </summary>
        public override void Add(IonValue item)
        {
            base.Add(item);
            Debug.Assert(item != null, nameof(item) + " != null");
            if (_symbolTables.Count > 0)
            {
                item._tableIndex = (short) _symbolTables.Count;
            }
        }

        public override void Insert(int index, IonValue item)
        {
            //first get the item at that index
            short tableIndex = -1;
            if (index < _children.Count)
            {
                tableIndex = _children[index]._tableIndex;
            }

            //insert
            base.Insert(index, item);
            //set the child's table index to the previous one
            Debug.Assert(item != null);
            item._tableIndex = tableIndex;
        }

        internal ISymbolTable GetSymbolTableForChild(IonValue child)
        {
            Debug.Assert(Contains(child));
            if (child._tableIndex < 0)
                return null;

            return _symbolTables[child._tableIndex];
        }

        internal void AppendSymbolTable(ISymbolTable symbolTable)
        {
            if (_symbolTables.Count > short.MaxValue)
                throw new IonException("Too many symbol tables");

            Debug.Assert(symbolTable != null);
            _symbolTables.Add(symbolTable);
        }
    }
}
