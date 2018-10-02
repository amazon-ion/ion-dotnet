using System;
using System.Collections.Generic;
using IonDotnet.Internals.Binary;

namespace IonDotnet.Internals
{
    internal sealed class SubstituteSymbolTable : ISymbolTable
    {
        private readonly ISymbolTable _original;

        public SubstituteSymbolTable(string name, int version, int maxId)
        {
            Name = name;
            Version = version;
            MaxId = maxId;
        }

        public SubstituteSymbolTable(ISymbolTable original, int version, int maxId) : this(original.Name, version, maxId)
        {
            _original = original;
        }

        public int MaxId { get; }
        public string Name { get; }
        public int Version { get; }
        public bool IsLocal => false;
        public bool IsShared => true;
        public bool IsSubstitute => false;
        public bool IsSystem => false;
        public bool IsReadOnly => true;

        public void MakeReadOnly()
        {
        }

        public ISymbolTable GetSystemTable() => null;

        public string IonVersionId => null;

        public IEnumerable<ISymbolTable> GetImportedTables()
        {
            yield break;
        }

        public int GetImportedMaxId() => 0;


        public SymbolToken Intern(string text)
        {
            throw new InvalidOperationException("Cannot add symbol to read-only tables.");
        }

        public SymbolToken Find(string text)
        {
            if (_original == null)
                return default;

            var token = _original.Find(text);
            return token == default || token.Sid > MaxId ? default : token;
        }

        public int FindSymbolId(string text)
        {
            var id = _original?.FindSymbolId(text) ?? SymbolToken.UnknownSid;
            return id <= MaxId ? id : SymbolToken.UnknownSid;
        }

        public string FindKnownSymbol(int sid)
        {
            if (sid > MaxId || _original == null)
                return null;
            return _original.FindKnownSymbol(sid);
        }

        public void WriteTo(IIonWriter writer)
        {
            var reader = new SymbolTableReader(this);
            writer.WriteValues(reader);
        }

        public IIterator<string> IterateDeclaredSymbolNames()
        {
            throw new NotImplementedException();
        }
    }
}
