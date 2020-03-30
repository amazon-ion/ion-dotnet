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

namespace Amazon.IonDotnet.Internals.Binary
{
    using System.Diagnostics;
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// This user-level reader is used to recognize symbols and process symbol table.
    /// </summary>
    /// <remarks>Starts out as a system bin reader.</remarks>
    internal sealed class UserBinaryReader : SystemBinaryReader
    {
        private readonly ICatalog catalog;

        internal UserBinaryReader(Stream input, ICatalog catalog = null)
            : base(input)
        {
            this.catalog = catalog;
        }

        public override IonType MoveNext()
        {
            this.GetSymbolTable();
            if (!this.HasNext())
            {
                return IonType.None;
            }

            this.moveNextNeeded = true;
            return this.valueType;
        }

        protected override bool HasNext()
        {
            if (this.eof || !this.moveNextNeeded)
            {
                return !this.eof;
            }

            while (!this.eof && this.moveNextNeeded)
            {
                this.MoveNextUser();
            }

            return !this.eof;
        }

        private void MoveNextUser()
        {
            base.HasNext();

            // if we're not at the top (datagram) level or the next value is null
            if (this.CurrentDepth != 0 || this.valueIsNull)
            {
                return;
            }

            Debug.Assert(this.valueTid != BinaryConstants.TidTypedecl, "valueTid is Typedec1");

            if (this.valueTid == BinaryConstants.TidSymbol)
            {
                // trying to read a symbol here
                // $ion_1_0 is read as an IVM only if it is not annotated
                // we already count the number of annotations
                if (this.Annotations.Count != 0)
                {
                    return;
                }

                this.LoadOnce();

                // just get it straight from the holder, no conversion needed
                var sid = this.valueVariant.IntValue;
                if (sid != SystemSymbols.Ion10Sid)
                {
                    return;
                }

                this.SymbolTable = SharedSymbolTable.GetSystem(1);

                // user don't need to see this symbol so continue here
                this.moveNextNeeded = true;
            }
            else if (this.valueTid == BinaryConstants.TidStruct)
            {
                // trying to read the local symbolTable here
                if (this.hasSymbolTableAnnotation)
                {
                    this.SymbolTable = ReaderLocalTable.ImportReaderTable(this, this.catalog, false);

                    // user don't need to read the localsymboltable so continue
                    this.moveNextNeeded = true;
                }
            }
        }
    }
}
