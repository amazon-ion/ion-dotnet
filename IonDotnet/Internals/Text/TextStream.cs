using System;

namespace IonDotnet.Internals.Text
{
    /// <summary>
    /// A unified stream that can represent both text and Utf-8 bytes
    /// </summary>
    /// <remarks>This stream is not an <see cref="IDisposable"/> and does not any underlying stream(s).</remarks>
    internal abstract class TextStream
    {
        /// <summary>
        /// Read an 'unit', which, depending on the kind of stream, might be a 2-byte <see cref="char"/> 
        /// or a <see cref="byte"/>
        /// </summary>
        /// <returns>The next unit read</returns>
        public abstract int Read();

        /// <summary>
        /// Try to unread the character unit
        /// </summary>
        /// <param name="c">Has to be most-recently read unit</param>
        public abstract void Unread(int c);
    }
}
