using System.Threading.Tasks;

namespace IonDotnet.Internals.Text
{
    //TODO is there any beter way than this?

    /// <summary>
    /// A unified stream that can represent both text and 
    /// </summary>
    internal abstract class TextStream
    {
        /// <summary>
        /// True if this is a byte stream
        /// </summary>
        public abstract bool IsByteStream { get; }

        /// <summary>
        /// Read an 'unit', which, depending on the kind of stream, might be a 2-byte <see cref="char"/> 
        /// or a <see cref="byte"/>
        /// </summary>
        /// <returns>The unit read</returns>
        public abstract int Read();
    }
}
