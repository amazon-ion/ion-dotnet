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

using System.Diagnostics;
using System.IO;

namespace Amazon.IonDotnet.Internals.Binary
{
    /// <inheritdoc />
    /// <summary>
    /// This user-level reader is used to recognize symbols and process symbol table.
    /// </summary>
    /// <remarks>Starts out as a system bin reader</remarks>
    internal sealed class UserBinaryReader : SystemBinaryReader
    {
        private readonly ICatalog _catalog;

        internal UserBinaryReader(Stream input, ICatalog catalog = null)
            : base(input)
        {
            _catalog = catalog;
        }

        public override IonType MoveNext()
        {
            GetSymbolTable();
            if (!HasNext()) return IonType.None;
            moveNextNeeded = true;
            return valueType;
        }

        protected override bool HasNext()
        {
            if (eof || !moveNextNeeded)
                return !eof;

            while (!eof && moveNextNeeded)
            {
                MoveNextUser();
            }

            return !eof;
        }

        private void MoveNextUser()
        {
            base.HasNext();

            // if we're not at the top (datagram) level or the next value is null
            if (CurrentDepth != 0 || valueIsNull) 
                return;
            Debug.Assert(valueTid != BinaryConstants.TidTypedecl);

            if (valueTid == BinaryConstants.TidSymbol)
            {
                // trying to read a symbol here
                // $ion_1_0 is read as an IVM only if it is not annotated
                // we already count the number of annotations
                if (Annotations.Count != 0)
                    return;

                LoadOnce();

                // just get it straight from the holder, no conversion needed
                var sid = valueVariant.IntValue;
                if (sid != SystemSymbols.Ion10Sid)
                    return;

                SymbolTable = SharedSymbolTable.GetSystem(1);
                //user don't need to see this symbol so continue here
                moveNextNeeded = true;
            }
            else if (valueTid == BinaryConstants.TidStruct)
            {
                //trying to read the local symboltable here
                if (hasSymbolTableAnnotation)
                {
                    SymbolTable = ReaderLocalTable.ImportReaderTable(this, _catalog, false);
                    //user don't need to read the localsymboltable so continue
                    moveNextNeeded = true;
                }
            }
        }
    }
}
