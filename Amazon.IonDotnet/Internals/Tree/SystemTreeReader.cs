/*
 * Copyright 2020 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Amazon.IonDotnet.Tree;
using Amazon.IonDotnet.Tree.Impl;

namespace Amazon.IonDotnet.Internals.Tree
{
    internal class SystemTreeReader : IIonReader
    {
        protected readonly ISymbolTable _systemSymbols;
        protected IEnumerator<IIonValue> _iter;
        protected IIonValue _parent;
        protected IIonValue _next;
        protected IIonValue _current;
        protected bool _eof;
        protected int _top;
        // Holds pairs: IonValue parent (_parent), Iterator<IIonValue> cursor (_iter)
        private Object[] _stack = new Object[10];
        private int pos = 0;

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
                _parent = null;
                _next = value;
            }
        }

        public int CurrentDepth => _top/2;

        public IonType CurrentType => (_current == null) ? IonType.None : _current.Type();

        public string CurrentFieldName => _current.FieldNameSymbol.Text;

        public bool CurrentIsNull => _current.IsNull;

        public bool IsInStruct => CurrentDepth > 0 && _parent != null && _parent.Type() == IonType.Struct;

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
                pos += lobSize;
                return lobSize;
            }
            else if (lobSize > bufSize && pos <= lobSize)
            {
                _current.Bytes()
                    .Slice(pos, bufSize)
                    .CopyTo(buffer);
                pos += bufSize;
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

        public string[] GetTypeAnnotations()
        {
            IReadOnlyCollection<SymbolToken> symbolTokens = _current.GetTypeAnnotationSymbols();

            string[] annotations = new string[symbolTokens.Count];

            int index = 0;
            foreach (SymbolToken symbolToken in symbolTokens)
            {
                if (symbolToken.Text == null)
                {
                    throw new UnknownSymbolException(symbolToken.Sid);
                }
                annotations[index] = symbolToken.Text;
                index++;
            }

            return annotations;
        }

        public IEnumerable<SymbolToken> GetTypeAnnotationSymbols()
        {
            return _current.GetTypeAnnotationSymbols();
        }

        public bool HasAnnotation(string annotation)
        {
            if (annotation == null)
            {
                throw new ArgumentNullException(nameof(annotation));
            }

            int? symbolTokenId = null;
            foreach (SymbolToken symbolToken in _current.GetTypeAnnotationSymbols())
            {
                string text = symbolToken.Text;
                if (text == null)
                {
                    symbolTokenId = symbolToken.Sid;
                }

                if (annotation.Equals(text))
                {
                    return true;
                }
            }

            if (symbolTokenId.HasValue)
            {
                throw new UnknownSymbolException(symbolTokenId.Value);
            }

            return false;
        }

        public int IntValue()
        {
            return _current.IntValue;
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
                return IonType.None;
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
            int oldLen = _stack.Length;
            if (_top + 1 >= oldLen)
            {
                // we're going to do a "+2" on top so we need extra space
                int newLen = oldLen * 2;
                Object[] temp = new Object[newLen];
                Array.Copy(_stack, 0, temp, 0, oldLen);
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
            IonType nextType = NextHelperSystem();
            return (nextType != IonType.None);
        }

        protected IonType NextHelperSystem()
        {
            if (_eof) return IonType.None;
            if (_next != null) return _next.Type();

            if(_iter != null && _iter.MoveNext())
            {
                _next = _iter.Current;
            }

            if ((_eof = (_next == null)))
            {
                return IonType.None;
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

        /// <summary>
        /// Dispose SystemTreeReader.
        /// </summary>
        public virtual void Dispose()
        {
            return;
        }

        internal class Children : IEnumerator<IIonValue>
        {
            bool _eof;
            int _nextIdx;
            readonly IIonValue _parent;
            IIonValue _curr;

            public Children(IIonValue parent)
            {
                _parent = parent;
                _nextIdx = -1;
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
                        return _parent.GetElementAt(_nextIdx);
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
                    _curr = null;
                    return false;
                }

                if (_nextIdx >= _parent.Count - 1)
                {
                    _eof = true;
                }
                else
                {
                    _curr = _parent.GetElementAt(++_nextIdx);
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
