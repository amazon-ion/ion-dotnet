using System;

namespace IonDotnet
{
    /// <summary>
    /// Defines the static constant Ion types.
    /// </summary>
    public class IonType
    {
        public static readonly IonType Null = new IonType("null", 0);
        public static readonly IonType Bool = new IonType("bool", 1);
        // note that INT is actually 0x2 **and** 0x3 in the Ion binary encoding
        public static readonly IonType Int = new IonType("int", 2);
        public static readonly IonType Float = new IonType("float", 4);
        public static readonly IonType Decimal = new IonType("decimals", 5);
        public static readonly IonType Timestamp = new IonType("timestamp", 6);
        public static readonly IonType Symbol = new IonType("symbol", 7);
        public static readonly IonType String = new IonType("string", 8);
        public static readonly IonType Clob = new IonType("clob", 9);
        public static readonly IonType Blob = new IonType("blob", 10);
        public static readonly IonType List = new IonType("list", 11);
        public static readonly IonType Sexp = new IonType("sexp", 12);
        public static readonly IonType Struct = new IonType("struct", 13);
        public static readonly IonType Datagram = new IonType("datagram", 14);

        public string Name { get; private set; }
        public int Id { get; private set; }

        private IonType(string name, int id)
        {
            Name = name;
            Id = id;
        }
    }

    public static class IonTypeExtensions
    {
        /// <summary>
        /// Determines whether a type represents an Ion container.
        /// </summary>
        /// <param name="t">IonType enum</param>
        /// <returns>true when t is enum after List</returns>
        public static bool IsContainer(this IonType t) => t.Id >= IonType.List.Id;

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
        public static bool IsScalar(this IonType t) => t.Id > IonType.Null.Id && t.Id < IonType.Blob.Id;

        /// <summary>
        /// Determines whether a type represents a numeric type
        /// </summary>
        /// <param name="t">IonType enum</param>
        /// <returns>true when this type is numeric</returns>
        public static bool IsNumeric(this IonType t) => t == IonType.Int || t == IonType.Float || t == IonType.Decimal;
    }
}
