namespace IonDotnet
{
    /// <summary>
    /// Indicates the smallest-possible C# type of an IonInt value
    /// </summary>
    public enum IntegerSize : int
    {
        /// <summary>
        /// Unknown
        /// </summary>
        Unknown = -1,

        /// <summary>
        /// Fit in C# 4-byte int
        /// </summary>
        Int = 0,

        /// <summary>
        /// Fit in C# 8-byte long
        /// </summary>
        Long = 1,

        /// <summary>
        /// Larger than 8-byte values
        /// </summary>
        BigInteger = 2,
    }
}
