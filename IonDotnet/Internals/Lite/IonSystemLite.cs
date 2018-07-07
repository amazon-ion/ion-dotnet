using System;
using System.Collections.Generic;
using System.IO;

namespace IonDotnet.Internals.Lite
{
    internal sealed class IonSystemLite : ValueFactoryLite, IPrivateIonSystem
    {
        public override T Clone<T>(T value)
        {
            throw new NotImplementedException();
        }

        public override IIonBlob NewNullBlob()
        {
            throw new NotImplementedException();
        }

        public override IIonBlob NewBlob(byte[] bytes)
        {
            throw new NotImplementedException();
        }

        public override IIonBlob NewBlob(ArraySegment<byte> bytes)
        {
            throw new NotImplementedException();
        }

        public override IIonBlob NewBlob(Span<byte> bytes)
        {
            throw new NotImplementedException();
        }

        public override IIonBool NewNullBool()
        {
            throw new NotImplementedException();
        }

        public override IIonBool NewBool(bool value)
        {
            throw new NotImplementedException();
        }

        public override IIonClob NewNullClob()
        {
            throw new NotImplementedException();
        }

        public override IIonClob NewClob(byte[] data)
        {
            throw new NotImplementedException();
        }

        public override IIonClob NewClob(ArraySegment<byte> data)
        {
            throw new NotImplementedException();
        }

        public override IIonClob NewClob(Span<byte> data)
        {
            throw new NotImplementedException();
        }

        public override IIonDecimal NewNullDecimal()
        {
            throw new NotImplementedException();
        }

        public override IIonDecimal NewDecimal(decimal value)
        {
            throw new NotImplementedException();
        }

        public override IIonDecimal NewDecimal(long value)
        {
            throw new NotImplementedException();
        }

        public override IIonDecimal NewDecimal(double value)
        {
            throw new NotImplementedException();
        }

        public override IIonFloat NewNullFloat()
        {
            throw new NotImplementedException();
        }

        public override IIonFloat NewFloat(double value)
        {
            throw new NotImplementedException();
        }

        public override IIonFloat NewFloat(long value)
        {
            throw new NotImplementedException();
        }

        public override IIonInt NewNullInt()
        {
            throw new NotImplementedException();
        }

        public override IIonInt NewInt(int value)
        {
            throw new NotImplementedException();
        }

        public override IIonInt NewInt(long value)
        {
            throw new NotImplementedException();
        }

        public override IIonList NewNullList()
        {
            throw new NotImplementedException();
        }

        public override IIonList NewEmptyList()
        {
            throw new NotImplementedException();
        }

        public override IIonList NewList(IIonSequence children)
        {
            throw new NotImplementedException();
        }

        public override IIonList NewList(params IIonValue[] children)
        {
            throw new NotImplementedException();
        }

        public override IIonList NewList(IEnumerable<int> values)
        {
            throw new NotImplementedException();
        }

        public override IIonList NewList(IEnumerable<long> values)
        {
            throw new NotImplementedException();
        }

        public override IIonNull NewNull()
        {
            throw new NotImplementedException();
        }

        public override IIonValue NewNull(IonType type)
        {
            throw new NotImplementedException();
        }

        public override IIonValue NewNullString()
        {
            throw new NotImplementedException();
        }

        public override IIonValue NewString(string value)
        {
            throw new NotImplementedException();
        }

        public override IIonValue NewString(Span<char> value)
        {
            throw new NotImplementedException();
        }

        public override IIonStruct NewNullStruct()
        {
            throw new NotImplementedException();
        }

        public override IIonStruct NewStruct()
        {
            throw new NotImplementedException();
        }

        public override IIonSymbol NewNullSymbol()
        {
            throw new NotImplementedException();
        }

        public override IIonSymbol NewSymbol(string text)
        {
            throw new NotImplementedException();
        }

        public override IIonSymbol NewSymbol(SymbolToken token)
        {
            throw new NotImplementedException();
        }

        public override IIonTimestamp NewNullTimestamp()
        {
            throw new NotImplementedException();
        }

        public override IIonTimestamp NewTimestamp(DateTimeOffset dateTimeOffset)
        {
            throw new NotImplementedException();
        }

        public ISymbolTable GetSystemSymbolTable()
        {
            throw new NotImplementedException();
        }

        public ISymbolTable GetSystemSymbolTable(string ionVersionId)
        {
            throw new NotImplementedException();
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
