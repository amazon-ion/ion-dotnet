namespace IonDotnet
{
    /// <summary>
    /// A SymbolToken providing both the symbol text and the assigned symbol ID. 
    ///  Symbol tokens may be interned into a <see cref="ISymbolTable"/> <br/>
    /// </summary>
    /// <remarks>
    /// This is implemented differently from the java implemenation using a readonly struct 
    /// to avoid creating heap objects
    /// </remarks>
    public readonly struct SymbolToken
    {
        public const int UnknownSid = -1;
        public static readonly SymbolToken None = default;
        public static readonly SymbolToken[] EmptyArray = new SymbolToken[0];

        private readonly int _sid;

        /// <summary>
        /// Create a new symbol token
        /// </summary>
        /// <param name="text">Text</param>
        /// <param name="sid">Sid</param>
        public SymbolToken(string text, int sid)
        {
            /**
             * Note: due to the fact that C# structs are initialized 'blank' (all fields 0), and we want the default
             * Sid to be Unknown(-1), the actual field value is shifted by +1 compared to the publicly
             * returned value
             */

            Text = text;
            _sid = sid + 1;
        }

        /// <summary>
        /// The text of this symbol.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// The ID of this symbol token.
        /// </summary>
        public int Sid => _sid - 1;

        /// <returns>Symbol Text</returns>
        /// <exception cref="UnknownSymbolException">If the text is null</exception>
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
