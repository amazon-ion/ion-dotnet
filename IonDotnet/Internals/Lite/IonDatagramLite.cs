using System.Collections.Generic;
using System.IO;

namespace IonDotnet.Internals.Lite
{
    internal class IonDatagramLite : IonSequenceLite, IPrivateDatagram
    {
        public IonDatagramLite(ContainerlessContext containerlessContext, bool isNull) : base(containerlessContext, isNull)
        {
        }

        public IonDatagramLite(IonContainerLite existing, IContext context, bool isStruct) : base(existing, context, isStruct)
        {
        }

        protected override int GetHashCode(ISymbolTableProvider symbolTableProvider)
        {
            throw new System.NotImplementedException();
        }

        public override IonValueLite Clone(IContext parentContext)
        {
            throw new System.NotImplementedException();
        }

        protected override void WriteBodyTo(IIonWriter writer, ISymbolTableProvider symbolTableProvider)
        {
            throw new System.NotImplementedException();
        }

        public override IonType Type { get; }
        public override void Accept(IValueVisitor visitor)
        {
            throw new System.NotImplementedException();
        }

        public override int IndexOf(IIonValue item)
        {
            throw new System.NotImplementedException();
        }

        public override void Insert(int index, IIonValue item)
        {
            throw new System.NotImplementedException();
        }

        public override void RemoveAt(int index)
        {
            throw new System.NotImplementedException();
        }

        public override IIonValue this[int index]
        {
            get => throw new System.NotImplementedException();
            set => throw new System.NotImplementedException();
        }

        public override IValueFactory Add()
        {
            throw new System.NotImplementedException();
        }

        public int SystemSize { get; }
        public IIonValue SystemGet(int index)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<IIonValue> GetSystemEnumerable()
        {
            throw new System.NotImplementedException();
        }

        public int ByteSize { get; }
        public byte[] GetBytes()
        {
            throw new System.NotImplementedException();
        }

        public int WriteBytes(Stream stream)
        {
            throw new System.NotImplementedException();
        }

        public IIonDatagram Clone()
        {
            throw new System.NotImplementedException();
        }

        public void AppendTrailingSymbolTable(ISymbolTable symtab)
        {
            throw new System.NotImplementedException();
        }
    }
}
