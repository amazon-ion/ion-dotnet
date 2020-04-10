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
    using Amazon.IonDotnet.Tree;

    internal class UserTreeReader : SystemTreeReader
    {
        // The ID of system symbol {@value #ION_1_0}, as defined by Ion 1.0.
        private const int Ion10Sid = 2;
        private readonly ICatalog catalog;
        private int symbolTableTop = 0;
        private ISymbolTable[] symbolTableStack = new ISymbolTable[3]; // 3 is rare, IVM followed by a local sym tab with open content

        public UserTreeReader(IIonValue value, ICatalog catalog = null)
            : base(value)
        {
            this.catalog = catalog;
        }

        public override ISymbolTable GetSymbolTable() => throw new InvalidOperationException("This operation is not supported.");

        public override bool HasNext()
        {
            return this.NextHelperUser();
        }

        public override IonType MoveNext()
        {
            if (!this.NextHelperUser())
            {
                this.current = null;
                return IonType.None;
            }

            this.current = this.next;
            this.next = null;
            return this.current.Type();
        }

        private bool NextHelperUser()
        {
            if (this.eof)
            {
                return false;
            }

            if (this.next != null)
            {
                return true;
            }

            this.ClearSystemValueStack();

            // read values from the system
            // reader and if they are system values
            // process them. Return when we've
            // read all the immediate system values
            IonType nextType;
            while (true)
            {
                nextType = this.NextHelperSystem();

                if (this.top == 0 && this.parent != null && this.parent.Type() == IonType.Datagram)
                {
                    if (nextType == IonType.Symbol)
                    {
                        var sym = this.next;
                        if (sym.IsNull)
                        {
                            // there are no null values that we will consume here
                            break;
                        }

                        int sid = sym.SymbolValue.Sid;

                        // if sid is unknown
                        if (sid == -1)
                        {
                            string name = sym.SymbolValue.Text;
                            if (name != null)
                            {
                                sid = this.systemSymbols.FindSymbolId(name);
                            }
                        }

                        if (sid == Ion10Sid && this.next.GetTypeAnnotationSymbols().Count == 0)
                        {
                            // $ion_1_0 is read as an IVM only if it is not annotated
                            ISymbolTable symbols = this.systemSymbols;
                            this.PushSymbolTable(symbols);
                            this.next = null;
                            continue;
                        }
                    }
                    else if (nextType == IonType.Struct && this.next.HasAnnotation("$ion_symbol_table"))
                    {
                        // read a local symbol table
                        ISymbolTable symtab = ReaderLocalTable.ImportReaderTable(this, this.catalog, true);
                        this.PushSymbolTable(symtab);
                        this.next = null;
                        continue;
                    }
                }

                // if we get here we didn't process a system
                // value, if we had we would have 'continue'd
                // so this is a value the user gets
                break;
            }

            return nextType != IonType.None;
        }

        private void ClearSystemValueStack()
        {
            while (this.symbolTableTop > 0)
            {
                this.symbolTableTop--;
                this.symbolTableStack[this.symbolTableTop] = null;
            }
        }

        private void PushSymbolTable(ISymbolTable symbols)
        {
            if (this.symbolTableTop >= this.symbolTableStack.Length)
            {
                int new_len = this.symbolTableStack.Length * 2;
                ISymbolTable[] temp = new ISymbolTable[new_len];
                Array.Copy(this.symbolTableStack, 0, temp, 0, this.symbolTableStack.Length);
                this.symbolTableStack = temp;
            }

            this.symbolTableStack[this.symbolTableTop++] = symbols;
        }
    }
}
