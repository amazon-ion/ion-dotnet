using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using IonDotnet.Internals.Binary;
using IonDotnet.Systems;

namespace IonDotnet.Internals.Lite
{
    internal sealed class IonSystemLite : ValueFactoryLite, IPrivateIonSystem
    {
        private readonly ISymbolTable _systemSymbolTable = null;

        public ISymbolTable GetSystemSymbolTable() => _systemSymbolTable;

        public ISymbolTable GetSystemSymbolTable(string ionVersionId)
        {
            if (ionVersionId != SystemSymbols.Ion10) throw new UnsupportedIonVersionException(ionVersionId);
            return _systemSymbolTable;
        }

        public ICatalog Catalog { get; }

        public ISymbolTable NewLocalSymbolTable(params ISymbolTable[] imports)
        {
            throw new NotImplementedException();
        }

        public ISymbolTable NewSharedSymbolTable(string name, int version, IEnumerable<string> newSymbols, params ISymbolTable[] imports)
        {
            throw new NotImplementedException();
        }

        public ISymbolTable NewSharedSymbolTable(IIonReader reader)
        {
            throw new NotImplementedException();
        }

        public ILoader NewLoader()
        {
            throw new NotImplementedException();
        }

        public ILoader NewLoader(ICatalog catalog)
        {
            throw new NotImplementedException();
        }

        public ILoader GetLoader()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IIonValue> Enumerate(TextReader textReader)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IIonValue> Enumerate(Stream stream)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IIonValue> Enumerate(byte[] ionData)
        {
            throw new NotImplementedException();
        }

        public IIonValue SingleValue(string ionText)
        {
            throw new NotImplementedException();
        }

        public IIonValue SingleValue(byte[] ionData)
        {
            throw new NotImplementedException();
        }

        public IIonReader NewReader(string ionText)
        {
            throw new NotImplementedException();
        }

        public IIonReader NewReader(byte[] ionData)
        {
            throw new NotImplementedException();
        }

        public IIonReader NewReader(ArraySegment<byte> ionData)
        {
            throw new NotImplementedException();
        }

        public IIonReader NewReader(Stream ionData)
        {
            throw new NotImplementedException();
        }

        public IIonReader NewReader(TextReader ionText)
        {
            throw new NotImplementedException();
        }

        public IIonReader NewReader(IIonValue value)
        {
            throw new NotImplementedException();
        }

        public IIonWriter NewWriter(IIonContainer container)
        {
            throw new NotImplementedException();
        }

        public IIonWriter NewTextWriter(Stream outputStream, params ISymbolTable[] imports)
        {
            throw new NotImplementedException();
        }

        public IIonWriter NewBinaryWriter(Stream outputStream, params ISymbolTable[] imports)
        {
            throw new NotImplementedException();
        }

        public IIonDatagram NewDatagram()
        {
            throw new NotImplementedException();
        }

        public IIonDatagram NewDatagram(IIonValue initialChild)
        {
            throw new NotImplementedException();
        }

        public IIonDatagram NewDatagram(params ISymbolTable[] imports)
        {
            throw new NotImplementedException();
        }

        public IIonValue NewValue(IIonReader reader)
        {
            throw new NotImplementedException();
        }
    }
}
