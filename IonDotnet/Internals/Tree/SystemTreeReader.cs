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
                _next = value;
            }
        }

        public int CurrentDepth => _top/2;

        public IonType CurrentType => _value.Type();

        public string CurrentFieldName => throw new NotImplementedException();

        public bool CurrentIsNull => _value.IsNull;

        public bool IsInStruct => CurrentDepth > 0 && _parent.Type() == IonType.Struct;

        public BigInteger BigIntegerValue()
        {
            return _value.BigIntegerValue;
        }

        public bool BoolValue()
        {
            return _value.BoolValue;
        }

        public BigDecimal DecimalValue()
        {
            return _value.BigDecimalValue;
        }

        public double DoubleValue()
        {
            return _value.DoubleValue;
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
                _value.Bytes().CopyTo(buffer);
                return lobSize;
            }
            else if (lobSize > bufSize)
            {
                _value.Bytes()
                    .Slice(0, bufSize - 1)
                    .CopyTo(buffer);
                return bufSize;
            }

            throw new IonException("Problem while copying the current blob value to a buffer");
        }

        public SymbolToken GetFieldNameSymbol()
        {
            return _value.FieldNameSymbol;
        }

        public IntegerSize GetIntegerSize()
        {
            return _value.IntegerSize;
        }

        public int GetLobByteSize()
        {
            return _value.ByteSize();
        }

        public virtual ISymbolTable GetSymbolTable() => _systemSymbols;

        public IEnumerable<SymbolToken> GetTypeAnnotations()
        {
            return _value.GetTypeAnnotations();
        }

        public int IntValue()
        {
            return _value.IntValue;
        }

        public long LongValue()
        {
            return _value.LongValue;
        }

        public IonType MoveNext()
        {
            if (_next == null && !hasNext())
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
            return _value.Bytes().ToArray();
        }

        public void StepIn()
        {
            if (!IsContainer())
            {
                throw new IonException("current value must be a container");
            }

            push();
            _parent = _current;
            _iter = _current;
            _current = null;
        }

        private bool IsContainer()
        {
            return _current.Type() == IonType.Struct
                || _current.Type() == IonType.List
                || _current.Type() == IonType.Sexp
                || _current.Type() == IonType.Datagram;
        }

        private void push()
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

        private void pop()
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

        public bool hasNext()
        {
            IonType next_type = next_helper_system();
            return (next_type != IonType.Null);
        }

        IonType next_helper_system()
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

        //TODO
        public void StepOut()
        {
            if (_top < 1)
            {
                throw new IonException("Cannot stepOut any further, already at top level.");
            }
            pop();
            _current = null;
        }

        public string StringValue()
        {
            return _value.StringValue;
        }

        public SymbolToken SymbolValue()
        {
            return _value.SymbolValue;
        }

        public Timestamp TimestampValue()
        {
            return _value.TimestampValue;
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

            //TODO: FIX THE LOGIC
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
                //return !_eof;

                if (true)
                {
                    _next_idx++;
                    return true;
                }
                return false;
                // position++;
                // return (position < _people.Length);
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }
        }
    }
}
