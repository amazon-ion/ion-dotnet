using IonDotnet.Tree;
using IonDotnet.Tree.Impl;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace IonDotnet.Internals.Tree
{
    internal class SystemTreeReader : IIonReader
    {

        protected readonly IIonValue _value;
        protected readonly ISymbolTable _systemSymbols;
        protected IEnumerator<IIonValue> _iter;
        protected IIonValue _parent;
        protected IIonValue _next;
        protected IIonValue _current;
        protected bool _eof;
        protected int _top;
        private Object[] _stack = new Object[10];

        protected SystemTreeReader(IIonValue value)
        {
            _systemSymbols = SharedSymbolTable.GetSystem(1);
            _current = null;
            _eof = false;
            _top = 0;
            if (value.Type() == IonType.Datagram)
            {
                _parent = value;
                _next = null;
                _iter = value.GetEnumerator();
            }
            else
            {
                _parent = (IIonValue)value.Container;
                _current = value;
            }
        }

        public int CurrentDepth => _top/2;

        public IonType CurrentType => _current.Type();

        public string CurrentFieldName => throw new NotImplementedException();

        public bool CurrentIsNull => _current.IsNull;

        public bool IsInStruct => CurrentDepth > 0 && _parent.Type() == IonType.Struct;

        public BigInteger BigIntegerValue()
        {
            return _current.BigIntegerValue;
        }

        public bool BoolValue()
        {
            return _current.BoolValue;
        }

        public BigDecimal DecimalValue()
        {
            return _current.BigDecimalValue;
        }

        public double DoubleValue()
        {
            return _current.DoubleValue;
        }

        public int GetBytes(Span<byte> buffer)
        {
            var lobSize = GetLobByteSize();
            var bufSize = buffer.Length;

            if (lobSize < 0 || bufSize < 0)
            {
                return 0;
            }
            else if (lobSize <= bufSize)
            {
                _current.Bytes().CopyTo(buffer);
                return lobSize;
            }
            else if (lobSize > bufSize)
            {
                _current.Bytes()
                    .Slice(0, bufSize - 1)
                    .CopyTo(buffer);
                return bufSize;
            }

            throw new IonException("Problem while copying the current blob value to a buffer");
        }

        public SymbolToken GetFieldNameSymbol()
        {
            return _current.FieldNameSymbol;
        }

        public IntegerSize GetIntegerSize()
        {
            return _current.IntegerSize;
        }

        public int GetLobByteSize()
        {
            return _current.ByteSize();
        }

        public virtual ISymbolTable GetSymbolTable() => _systemSymbols;

        public IEnumerable<SymbolToken> GetTypeAnnotations()
        {
            return _current.GetTypeAnnotations();
        }

        public int IntValue()
        {
            return _next.IntValue;
        }

        public long LongValue()
        {
            return _current.LongValue;
        }

        public virtual IonType MoveNext()
        {
            if (_next == null && !HasNext())
            {
                _current = null;
                return IonType.Null;
            }
            _current = _next;
            _next = null;

            return ((IonValue)_current).Type();
        }

        public byte[] NewByteArray()
        {
            return _current.Bytes().ToArray();
        }

        public void StepIn()
        {
            if (!IsContainer())
            {
                throw new IonException("current value must be a container");
            }

            Push();
            _parent = _current;
            _iter = new Children(_current);
            _current = null;
        }

        private bool IsContainer()
        {
            return _current.Type() == IonType.Struct
                || _current.Type() == IonType.List
                || _current.Type() == IonType.Sexp
                || _current.Type() == IonType.Datagram;
        }

        private void Push()
        {
            int oldlen = _stack.Length;
            if (_top + 1 >= oldlen)
            {
                // we're going to do a "+2" on top so we need extra space
                int newlen = oldlen * 2;
                Object[] temp = new Object[newlen];
                Array.Copy(_stack, 0, temp, 0, oldlen);
                _stack = temp;
            }
            _stack[_top++] = _parent;
            _stack[_top++] = _iter;
        }

        private void Pop()
        {
            _top--;
            _iter = (IEnumerator<IIonValue>)_stack[_top];
            _stack[_top] = null;  // Allow iterator to be garbage collected!

            _top--;
            _parent = (IIonValue)_stack[_top];
            _stack[_top] = null;

            // We don't know if we're at the end of the container, so check again.
            _eof = false;
        }

        public virtual bool HasNext()
        {
            IonType next_type = NextHelperSystem();
            return (next_type != IonType.Null);
        }

        protected IonType NextHelperSystem()
        {
            if (_eof) return IonType.Null;
            if (_next != null) return _next.Type();

            while(_iter != null && _iter.MoveNext())
            {
                _next = _iter.Current;
            }

            if ((_eof = (_next == null)))
            {
                return IonType.Null;
            }
            return _next.Type();
        }

        public void StepOut()
        {
            if (_top < 1)
            {
                throw new IonException("Cannot stepOut any further, already at top level.");
            }
            Pop();
            _current = null;
        }

        public string StringValue()
        {
            return _current.StringValue;
        }

        public SymbolToken SymbolValue()
        {
            return _current.SymbolValue;
        }

        public Timestamp TimestampValue()
        {
            return _current.TimestampValue;
        }

        internal class Children : IEnumerator<IIonValue>
        {
            bool _eof;
            int _next_idx;
            IIonValue _parent;
            IIonValue _curr;

            public Children(IIonValue parent)
            {
                _parent = parent;
                _next_idx = 0;
                _curr = null;
                if (_parent.IsNull)
                {
                    // otherwise the empty contents member will cause trouble
                    _eof = true;
                }
            }

            public IIonValue Current
            {
                get
                {
                    try
                    {
                        return _parent.GetElementAt(_next_idx);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        throw new InvalidOperationException();
                    }
                }
            }

            object IEnumerator.Current => _curr;

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public bool MoveNext()
            {
                if (_eof)
                {
                    return false;
                }

                int len = _parent.Count;

                if (_next_idx > 0)
                {
                    int ii = _next_idx - 1;
                    _next_idx = len;

                    while (ii < len)
                    {
                        if (_curr == _parent.GetElementAt(ii))
                        {
                            _next_idx = ii + 1;
                            break;
                        }
                    }
                }
                if (_next_idx >= _parent.Count)
                {
                    _eof = true;
                }
                return !_eof;
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }
        }
    }
}
