namespace IonDotnet
{
    public enum IonType : int
    {
        None = -1,
        Null = 0,
        Bool = 1,
        Int = 2,
        Float = 3,
        Decimal = 4,
        Timestamp = 5,
        Symbol = 6,
        String = 7,
        Clob = 8,
        Blob = 9,
        List = 10,
        Sexp = 11,
        Struct = 12,
        Datagram = 13
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
