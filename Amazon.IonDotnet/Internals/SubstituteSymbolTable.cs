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

namespace Amazon.IonDotnet.Internals
{
    using System;
    using System.Collections.Generic;
    using Amazon.IonDotnet.Internals.Binary;

    internal sealed class SubstituteSymbolTable : ISymbolTable
    {
        private readonly ISymbolTable original;

        public SubstituteSymbolTable(string name, int version, int maxId)
        {
            this.Name = name;
            this.Version = version;
            this.MaxId = maxId;
        }

        public SubstituteSymbolTable(ISymbolTable original, int version, int maxId)
            : this(original.Name, version, maxId)
        {
            this.original = original;
        }

        public int MaxId { get; }

        public string Name { get; }

        public int Version { get; }

        public bool IsLocal => false;

        public bool IsShared => true;

        public bool IsSubstitute => true;

        public bool IsSystem => false;

        public bool IsReadOnly => true;

        public string IonVersionId => null;

        public void MakeReadOnly()
        {
        }

        public ISymbolTable GetSystemTable() => null;

        public IReadOnlyList<ISymbolTable> GetImportedTables() => SharedSymbolTable.EmptyArray;

        public int GetImportedMaxId() => 0;

        SymbolToken ISymbolTable.Intern(string text) => throw new InvalidOperationException("Cannot add symbol to read-only tables.");

        public SymbolToken Find(string text)
        {
            if (this.original == null)
            {
                return default;
            }

            var token = this.original.Find(text);
            return token == default || token.Sid > this.MaxId ? default : token;
        }

        public int FindSymbolId(string text)
        {
            var id = this.original?.FindSymbolId(text) ?? SymbolToken.UnknownSid;
            return id <= this.MaxId ? id : SymbolToken.UnknownSid;
        }

        public string FindKnownSymbol(int sid)
        {
            if (sid > this.MaxId || this.original == null)
            {
                return null;
            }

            return this.original.FindKnownSymbol(sid);
        }

        public void WriteTo(IIonWriter writer)
        {
            var reader = new SymbolTableReader(this);
            writer.WriteValues(reader);
        }

        public IEnumerable<string> GetDeclaredSymbolNames()
        {
            yield break;
        }

        public IEnumerable<string> GetOriginalSymbols()
        {
            if (this.original == null)
            {
                yield break;
            }

            foreach (var s in this.original.GetDeclaredSymbolNames())
            {
                yield return s;
            }
        }
    }
}
