using System.IO;
using IonDotnet.Systems;

namespace IonDotnet.Internals.Binary
{
    internal class PrivateIonBinaryWriterBuilder : IonBinaryWriterBuilder
    {
        public PrivateIonBinaryWriterBuilder(IonBinaryWriterBuilder that) : base(that)
        {
        }

        public override IIonWriter Build(Stream output)
        {
            throw new System.NotImplementedException();
        }

        public override IonBinaryWriterBuilder Copy()
        {
            throw new System.NotImplementedException();
        }

        public override IonBinaryWriterBuilder Immutable()
        {
            throw new System.NotImplementedException();
        }

        protected override IonBinaryWriterBuilder Mutable()
        {
            throw new System.NotImplementedException();
        }

        public override ISymbolTable InitialSymbolTable { get; set; }

        public override IonBinaryWriterBuilder WithInitialSymbolTable(ISymbolTable symtab)
        {
            throw new System.NotImplementedException();
        }

        public override bool IsFloatBinary32Enabled
        {
            set => throw new System.NotImplementedException();
        }

        public override IonBinaryWriterBuilder WithFloatBinary32Enabled()
        {
            throw new System.NotImplementedException();
        }

        public override IonBinaryWriterBuilder WithFloatBinary32Disabled()
        {
            throw new System.NotImplementedException();
        }

        public IValueFactory SymtabValueFactory { get; set; }
    }
}
