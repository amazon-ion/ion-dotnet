namespace IonDotnet
{
    public struct SymbolToken
    {
        public const int UnknownSymbolId = -1;
        public static readonly SymbolToken None = new SymbolToken(null, UnknownSymbolId);

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

        public static bool operator ==(SymbolToken x, SymbolToken y) => x.Text == y.Text && x.Sid == y.Sid;

        public static bool operator !=(SymbolToken x, SymbolToken y) => !(x == y);

        public override bool Equals(object that)
        {
            if (!(that is SymbolToken st)) return false;
            return st == this;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Text != null ? Text.GetHashCode() : 0) * 397) ^ Sid;
            }
        }
    }
}
