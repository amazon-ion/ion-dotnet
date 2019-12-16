namespace IonDotnet
{
    public enum IonType : short
    {
        None = -1,
        Null = 0,
        Bool = 1,
        // note that INT is actually 0x2 **and** 0x3 in the Ion binary encoding
        Int = 2,
        Float = 4,
        Decimal = 5,
        Timestamp = 6,
        Symbol = 7,
        String = 8,
        Clob = 9,
        Blob = 10,
        List = 11,
        Sexp = 12,
        Struct = 13,
        Datagram = 14
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
        /// Determines whether a type represents an Ion LOB
        /// </summary>
        /// <param name="t">IonType enum</param>
        /// <returns>true when t is Blob or Clob</returns>
        public static bool IsLob(this IonType t) => t == IonType.Blob || t == IonType.Clob;

        /// <summary>
        /// Determines whether a type represents a scalar value type
        /// </summary>
        /// <param name="t">IonType enum</param>
        /// <returns>true when the this is a scalar type</returns>
        public static bool IsScalar(this IonType t) => t > IonType.None && t < IonType.Clob;

        /// <summary>
        /// Determines whether a type represents a numeric type
        /// </summary>
        /// <param name="t">IonType enum</param>
        /// <returns>true when this type is numeric</returns>
        public static bool IsNumeric(this IonType t) => t == IonType.Int || t == IonType.Float || t == IonType.Decimal;
    }
}
