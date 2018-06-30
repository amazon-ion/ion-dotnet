namespace IonDotnet
{
    public struct SymbolToken
    {
        public const int UnknownSymbolId = -1;

        public SymbolToken(string text, int sid)
        {
            Text = text;
            Sid = sid;
        }

        public string Text { get; }
        public int Sid { get; }

        public string AssumeText()
        {
            if (Text == null) throw new UnknownSymbolException(Sid);
            return Text;
        }

        public override string ToString() => $"SymbolToken::{{text:{Text},id:{Sid}}}";
    }
}
