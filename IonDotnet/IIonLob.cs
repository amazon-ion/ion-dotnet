using System;
using System.IO;

namespace IonDotnet
{
    public interface IIonLob<out T> : IIonValue<T> where T : IIonValue
    {
        Stream OpenInputStream();

        byte[] ToByteArray();

        void SetBytes(ArraySegment<byte> bytes);

        /// <summary>
        /// Size of the lob in bytes
        /// </summary>
        int Size { get; }
    }
}
