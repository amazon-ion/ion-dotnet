using System;
using IonDotnet.Internals;

namespace IonDotnet.Tree
{
    public sealed class IonSymbol : IonText
    {
        private int _sid = SymbolToken.UnknownSid;

        public IonSymbol(string text) : this(new SymbolToken(text, SymbolToken.UnknownSid))
        {
        }

        public IonSymbol(SymbolToken symbolToken) : base(symbolToken.Text)
        {
        }

        internal override void WriteBodyTo(IPrivateWriter writer)
        {
            if (NullFlagOn())
            {
                writer.WriteNull(IonType.Symbol);
                return;
            }

            writer.WriteSymbol(StringValue);
        }

        public override IonType Type => IonType.Symbol;

        public override string StringValue
        {
            get => base.StringValue;
            set
            {
                base.StringValue = value;
                _sid = SymbolToken.UnknownSid;
            }
        }

        public SymbolToken SymbolValue => new SymbolToken(_stringVal, ResolveSid());

        private int ResolveSid()
        {
            if (_sid > SymbolToken.UnknownSid)
                return _sid;

            throw new NotImplementedException("top-level symtab");
        }
    }
}
