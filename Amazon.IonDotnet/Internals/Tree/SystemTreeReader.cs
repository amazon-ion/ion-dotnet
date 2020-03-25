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

namespace Amazon.IonDotnet.Internals.Tree
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Numerics;
    using Amazon.IonDotnet.Tree;
    using Amazon.IonDotnet.Tree.Impl;

    internal class SystemTreeReader : IIonReader
    {
        protected readonly ISymbolTable systemSymbols;
        protected IEnumerator<IIonValue> iter;
        protected IIonValue parent;
        protected IIonValue next;
        protected IIonValue current;
        protected bool eof;
        protected int top;

        // Holds pairs: IonValue parent (parent), Iterator<IIonValue> cursor (iter)
        private object[] stack = new object[10];
        private int pos = 0;

        protected SystemTreeReader(IIonValue value)
        {
            this.systemSymbols = SharedSymbolTable.GetSystem(1);
            this.current = null;
            this.eof = false;
            this.top = 0;
            if (value.Type() == IonType.Datagram)
            {
                this.parent = value;
                this.next = null;
                this.iter = value.GetEnumerator();
            }
            else
            {
                this.parent = null;
                this.next = value;
            }
        }

        public int CurrentDepth => this.top / 2;

        public IonType CurrentType => (this.current == null) ? IonType.None : this.current.Type();

        public string CurrentFieldName => this.current.FieldNameSymbol.Text;

        public bool CurrentIsNull => this.current.IsNull;

        public bool IsInStruct => this.CurrentDepth > 0 && this.parent != null && this.parent.Type() == IonType.Struct;

        public BigInteger BigIntegerValue()
        {
            return this.current.BigIntegerValue;
        }

        public bool BoolValue()
        {
            return this.current.BoolValue;
        }

        public BigDecimal DecimalValue()
        {
            return this.current.BigDecimalValue;
        }

        public double DoubleValue()
        {
            return this.current.DoubleValue;
        }

        public int GetBytes(Span<byte> buffer)
        {
            var lobSize = this.GetLobByteSize();
            var bufSize = buffer.Length;

            if (lobSize < 0 || bufSize < 0)
            {
                return 0;
            }
            else if (lobSize <= bufSize)
            {
                this.current.Bytes().CopyTo(buffer);
                this.pos += lobSize;
                return lobSize;
            }
            else if (lobSize > bufSize && this.pos <= lobSize)
            {
                this.current.Bytes()
                    .Slice(this.pos, bufSize)
                    .CopyTo(buffer);
                this.pos += bufSize;
                return bufSize;
            }

            throw new IonException("Problem while copying the current blob value to a buffer");
        }

        public SymbolToken GetFieldNameSymbol()
        {
            return this.current.FieldNameSymbol;
        }

        public IntegerSize GetIntegerSize()
        {
            return this.current.IntegerSize;
        }

        public int GetLobByteSize()
        {
            return this.current.ByteSize();
        }

        public virtual ISymbolTable GetSymbolTable() => this.systemSymbols;

        public string[] GetTypeAnnotations()
        {
            IReadOnlyCollection<SymbolToken> symbolTokens = this.current.GetTypeAnnotationSymbols();

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
            return this.current.GetTypeAnnotationSymbols();
        }

        public bool HasAnnotation(string annotation)
        {
            if (annotation == null)
            {
                throw new ArgumentNullException(nameof(annotation));
            }

            int? symbolTokenId = null;
            foreach (SymbolToken symbolToken in this.current.GetTypeAnnotationSymbols())
            {
                string text = symbolToken.Text;
                if (text == null)
                {
                    symbolTokenId = symbolToken.Sid;
                }
                else if (annotation.Equals(text))
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
            return this.current.IntValue;
        }

        public long LongValue()
        {
            return this.current.LongValue;
        }

        public virtual IonType MoveNext()
        {
            if (this.next == null && !this.HasNext())
            {
                this.current = null;
                return IonType.None;
            }

            this.current = this.next;
            this.next = null;

            return ((IonValue)this.current).Type();
        }

        public byte[] NewByteArray()
        {
            return this.current.Bytes().ToArray();
        }

        public void StepIn()
        {
            if (!this.IsContainer())
            {
                throw new IonException("current value must be a container");
            }

            this.Push();
            this.parent = this.current;
            this.iter = new Children(this.current);
            this.current = null;
        }

        public virtual bool HasNext()
        {
            IonType nextType = this.NextHelperSystem();
            return nextType != IonType.None;
        }

        public void StepOut()
        {
            if (this.top < 1)
            {
                throw new IonException("Cannot stepOut any further, already at top level.");
            }

            this.Pop();
            this.current = null;
        }

        public string StringValue()
        {
            return this.current.StringValue;
        }

        public SymbolToken SymbolValue()
        {
            return this.current.SymbolValue;
        }

        public Timestamp TimestampValue()
        {
            return this.current.TimestampValue;
        }

        /// <summary>
        /// Dispose SystemTreeReader.
        /// </summary>
        public virtual void Dispose()
        {
            return;
        }

        protected IonType NextHelperSystem()
        {
            if (this.eof)
            {
                return IonType.None;
            }

            if (this.next != null)
            {
                return this.next.Type();
            }

            if (this.iter != null && this.iter.MoveNext())
            {
                this.next = this.iter.Current;
            }

            if (this.eof = this.next == null)
            {
                return IonType.None;
            }

            return this.next.Type();
        }

        private bool IsContainer()
        {
            return this.current.Type() == IonType.Struct
                || this.current.Type() == IonType.List
                || this.current.Type() == IonType.Sexp
                || this.current.Type() == IonType.Datagram;
        }

        private void Push()
        {
            int oldLen = this.stack.Length;
            if (this.top + 1 >= oldLen)
            {
                // we're going to do a "+2" on top so we need extra space
                int newLen = oldLen * 2;
                object[] temp = new object[newLen];
                Array.Copy(this.stack, 0, temp, 0, oldLen);
                this.stack = temp;
            }

            this.stack[this.top++] = this.parent;
            this.stack[this.top++] = this.iter;
        }

        private void Pop()
        {
            this.top--;
            this.iter = (IEnumerator<IIonValue>)this.stack[this.top];
            this.stack[this.top] = null;  // Allow iterator to be garbage collected

            this.top--;
            this.parent = (IIonValue)this.stack[this.top];
            this.stack[this.top] = null;

            // We don't know if we're at the end of the container, so check again.
            this.eof = false;
        }

        internal class Children : IEnumerator<IIonValue>
        {
            private readonly IIonValue parent;
            private bool eof;
            private int nextIdx;
            private IIonValue curr;

            public Children(IIonValue parent)
            {
                this.parent = parent;
                this.nextIdx = -1;
                this.curr = null;
                if (this.parent.IsNull)
                {
                    // otherwise the empty contents member will cause trouble
                    this.eof = true;
                }
            }

            public IIonValue Current
            {
                get
                {
                    try
                    {
                        return this.parent.GetElementAt(this.nextIdx);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        throw new InvalidOperationException();
                    }
                }
            }

            object IEnumerator.Current => this.curr;

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public bool MoveNext()
            {
                if (this.eof)
                {
                    this.curr = null;
                    return false;
                }

                if (this.nextIdx >= this.parent.Count - 1)
                {
                    this.eof = true;
                }
                else
                {
                    this.curr = this.parent.GetElementAt(++this.nextIdx);
                }

                return !this.eof;
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }
        }
    }
}
