namespace IonDotnet
{
    public enum IonType
    {
        None,
        Null,
        Bool,
        Int,
        Float,
        Decimal,
        Timestamp,
        Symbol,
        String,
        Clob,
        Blob,
        List,
        Sexp,
        Struct,
        Datagram
    }

    public static class IonTypeExtensions
    {
        /// <summary>
        /// Determines whether a type represents an Ion container.
        /// </summary>
        /// <param name="t">IonType enum</param>
        /// <returns>true when t is enum after List</returns>
        public static bool IsContainer(this IonType t) => t >= IonType.List;

        /// <summary>
        /// Determines whether a type represents an Ion text scalar, namely
        /// </summary>
        /// <param name="t">IonType enum</param>
        /// <returns>true when t is String or Symbol</returns>
        public static bool IsText(this IonType t) => t == IonType.String || t == IonType.Symbol;

        /// <summary>
        /// Determines whether a type represents an Ion LOB scalar, namely
        /// </summary>
        /// <param name="t">IonType enum</param>
        /// <returns>true when t is Blob or Clob</returns>
        public static bool IsLob(this IonType t) => t == IonType.Blob || t == IonType.Clob;
    }
}
