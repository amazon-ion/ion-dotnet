using System;
using System.Collections.Generic;
using System.IO;

namespace IonDotnet
{
    public interface IIonDatagram : IIonSequence
    {
        /// <summary>
        /// Number of elements in the datagram, including system elements 
        /// such as version markers and symbol tables
        /// </summary>
        int SystemSize { get; }

        /// <summary>
        /// Gets a selected element from this datagram, potentially getting a hidden system element (such as a symbol table).
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>the selected element; not Null</returns>
        IIonValue SystemGet(int index);

        IEnumerable<IIonValue> GetSystemEnumerable();

        /// <summary>
        /// Gets the number of bytes used to encode this datagram.
        /// </summary>
        int ByteSize { get; }

        /// <summary>
        /// Copies the binary-encoded form of this datagram into a new byte array.
        /// </summary>
        /// <returns></returns>
        byte[] GetBytes();

        /// <summary>
        /// Copies the binary-encoded form of this datagram to a specified stream.
        /// </summary>
        /// <param name="stream">output stream to which to write the data.</param>
        /// <returns>number of bytes written.</returns>
        int WriteBytes(Stream stream);

        IIonDatagram Clone();
    }
}
