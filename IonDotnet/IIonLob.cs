using System;
using System.IO;

namespace IonDotnet
{
    public interface IIonLob : IIonValue
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
