using System;
using System.Collections.Generic;

namespace IonDotnet.Internals.Binary
{
    internal abstract class AbstractSymbolTable : ISymbolTable
    {
        protected AbstractSymbolTable(string name, int version)
        {
            Name = name;
            Version = version;
        }

        public string Name { get; }
        public int Version { get; }
        public abstract bool IsLocal { get; }
        public abstract bool IsShared { get; }
        public abstract bool IsSubstitute { get; }
        public abstract bool IsSystem { get; }
        public abstract bool IsReadOnly { get; }
        public abstract void MakeReadOnly();
        public abstract ISymbolTable GetSystemTable();

        public string IonVersionId => SystemSymbols.Ion10;

        public abstract IEnumerable<ISymbolTable> GetImportedTables();
        public abstract int GetImportedMaxId();
        public abstract int MaxId { get; }
        public abstract SymbolToken Intern(string text);
        public abstract SymbolToken Find(string text);

        public int FindSymbolId(string text)
        {
            var token = Find(text);
            return token == default ? SymbolToken.UnknownSid : token.Sid;
        }

        public abstract string FindKnownSymbol(int sid);

        public void WriteTo(IIonWriter writer)
        {
            throw new NotImplementedException();
        }

        public abstract IIterator<string> IterateDeclaredSymbolNames();
    }
}
