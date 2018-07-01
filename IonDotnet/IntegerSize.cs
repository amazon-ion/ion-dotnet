namespace IonDotnet
{
    /// <summary>
    /// Indicates the smallest-possible C# type of an IonInt value
    /// </summary>
    public enum IntegerSize
    {
        /// <summary>
        /// Nothing
        /// </summary>
        None,
        
        /// <summary>
        /// Fit in C# 4-byte int
        /// </summary>
        Int,

        /// <summary>
        /// Fit in C# 8-byte long
        /// </summary>
        Long,

        /// <summary>
        /// Larger than 8-byte values
        /// </summary>
        BigInteger,
    }
}
