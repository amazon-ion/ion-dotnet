using System;
using System.Collections.Generic;

namespace IonDotnet.Internals
{
    /// <inheritdoc />
    /// <summary>
    /// Provide the functionalities as a buffer for writing Ion data
    /// </summary>
    internal interface IWriteBuffer : IDisposable
    {
        /// <summary>
        /// Write a string into the buffer as UTF8 bytes
        /// </summary>
        /// <param name="s">The string</param>
        /// <returns>Number of bytes written</returns>
        int WriteUtf8(string s);

        /// <summary>
        /// Write the whole byte segment into the buffer
        /// </summary>
        /// <param name="bytes">The byte segment</param>
        void WriteBytes(Span<byte> bytes);

        /// <summary>
        /// Mark the end of a write streak 
        /// </summary>
        /// <returns>The sequence of memory written in the streak</returns>
        IList<Memory<byte>> Wrapup();
    }
}
