using IonDotnet.Internals;

namespace IonDotnet.Tree.Impl
{
    internal sealed class IonSymbol : IonText, IIonSymbol
    {
        private int _sid;
        private ImportLocation _importLocation;

        public IonSymbol(string text, int sid = SymbolToken.UnknownSid, ImportLocation importLocation = default) : this(new SymbolToken(text, sid, importLocation))
        {
        }

        public IonSymbol(SymbolToken symbolToken) : base(symbolToken.Text, symbolToken == default)
        {
            _sid = symbolToken.Sid;
            _importLocation = symbolToken.ImportLocation;
        }

        private IonSymbol(bool isNull) : base(null, isNull)
        {
        }

        /// <summary>
        /// Returns a new null.symbol value.
        /// </summary>
        public static IonSymbol NewNull() => new IonSymbol(true);

        public override bool IsEquivalentTo(IIonValue other)
        {
            if (!base.IsEquivalentTo(other))
                return false;

            if (!(other is IonSymbol oSymbol))
                return false;

            if (NullFlagOn())
                return oSymbol.IsNull;

            return !oSymbol.IsNull && oSymbol.StringVal == StringValue;
        }

        internal override void WriteBodyTo(IPrivateWriter writer)
        {
            if (NullFlagOn())
            {
                writer.WriteNull(IonType.Symbol);
                return;
            }

            writer.WriteSymbolToken(SymbolValue);
        }

        public override IonType Type() => IonType.Symbol;

        public override string StringValue
        {
            get => base.StringValue;
        }

        public override SymbolToken SymbolValue
        {
            get => new SymbolToken(StringVal, _sid, _importLocation);
        }
    }
}
