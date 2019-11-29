using System;

namespace IonDotnet
{
    /// <inheritdoc cref="IEquatable{T}" />
    /// <summary>
    /// A SymbolToken providing both the symbol text and the assigned symbol ID. 
    ///  Symbol tokens may be interned into a <see cref="T:IonDotnet.ISymbolTable" /> <br />
    /// </summary>
    /// <remarks>
    /// A text=null or sid=-1 value might indicate that such field is unknown in the contextual symbol table.
    /// </remarks>
    public readonly struct SymbolToken : IEquatable<SymbolToken>
    {
        /// <summary>
        /// The default Sid, which is unknown
        /// </summary>
        public const int UnknownSid = -1;

        /// <summary>
        /// The default import location, corresponds to not_found/unknown
        /// </summary>
        public static readonly ImportLocation UnknownImportLocation = new ImportLocation(ImportLocation.UnknownImportName, UnknownSid);

        /// <summary>
        /// The default value, corresponds to not_found/unknown
        /// </summary>
        public static readonly SymbolToken None = default;

        public static readonly SymbolToken[] EmptyArray = new SymbolToken[0];

        private readonly int _sid;

        /// <summary>
        /// Create a new symbol token.
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
            ImportLocation = UnknownImportLocation;
        }

        /// <summary>
        /// Create a new symbol token.
        /// </summary>
        /// <param name="text">Text</param>
        /// <param name="sid">Sid</param>
        /// <param name="importLocation">ImportLocation</param>
        public SymbolToken(string text, int sid, ImportLocation importLocation)
        {
            /**
             * Note: due to the fact that C# structs are initialized 'blank' (all fields 0), and we want the default
             * Sid to be Unknown(-1), the actual field value is shifted by +1 compared to the publicly
             * returned value
             */

            Text = text;
            _sid = sid + 1;
            ImportLocation = importLocation;
        }

        /// <summary>
        /// The text of this symbol.
        /// </summary>
        public readonly string Text;

        /// <summary>
        /// The ID of this symbol token.
        /// </summary>
        public int Sid => _sid - 1;

        /// <summary>
        /// The import location of this symbol token.
        /// </summary>
        public readonly ImportLocation ImportLocation;

        //Override everything to avoid boxing allocation
        public override string ToString() => $"SymbolToken::{{text:{Text}, importLocation:{ImportLocation.ToString()}}}";

        public static bool operator ==(SymbolToken x, SymbolToken y) => x.Text == y.Text && x.ImportLocation == y.ImportLocation;

        public static bool operator !=(SymbolToken x, SymbolToken y) => !(x == y);

        public override bool Equals(object that) => that is SymbolToken token && Equals(token);

        public override int GetHashCode() => Text?.GetHashCode() ?? Sid;

        public bool Equals(SymbolToken other) => this == other;

        public bool IsEquivalentTo(SymbolToken other)
        {
            if (!(Text is null))
                return Text == other.Text;
            if (other.Text != null)
                return false;

            return other.ImportLocation == ImportLocation;
        }
    }
}
