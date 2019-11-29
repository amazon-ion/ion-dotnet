namespace IonDotnet
{
    public readonly struct ImportLocation
    {

        /// <summary>
        /// The default ImportName, which is unknown
        /// </summary>
        public const string UnknownImportName = default;

        /// <summary>
        /// The default value, corresponds to not_found/unknown
        /// </summary>
        public static readonly ImportLocation None = default;


        /// <summary>
        /// Create a new ImportLocation struct.
        /// </summary>
        /// <param name="importName">ImportName</param>
        /// <param name="sid">Sid</param>
        public ImportLocation(string importName, int sid)
        {
            ImportName = importName;
            Sid = sid;
        }

        /// <summary>
        /// The import name of this import location.
        /// </summary>
        public readonly string ImportName;

        /// <summary>
        /// The ID of this import location.
        /// </summary>
        public readonly int Sid;

        public override string ToString() => $"ImportLocation::{{importName:{ImportName}, id:{Sid}}}";

        public static bool operator ==(ImportLocation x, ImportLocation y) => x.ImportName == y.ImportName && x.Sid == y.Sid;

        public static bool operator !=(ImportLocation x, ImportLocation y) => !(x == y);

        public override bool Equals(object that) => that is ImportLocation token && Equals(token);

        public override int GetHashCode() => ImportName?.GetHashCode() ?? Sid;
    }
}
