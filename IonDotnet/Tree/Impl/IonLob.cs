using System;
using System.Diagnostics;

namespace IonDotnet.Tree.Impl
{
    /// <inheritdoc />
    /// <summary>
    /// A value that holds a blob of byte data.
    /// </summary>
    public abstract class IonLob : IonValue, IIonLob
    {
        protected byte[] ByteBuffer;

        protected IonLob() : base(true)
        {
        }

        protected IonLob(ReadOnlySpan<byte> bytes) : base(false)
        {
            SetBytes(bytes);
        }

        /// <summary>
        /// Get the view of the bytes in this blob.
        /// </summary>
        /// <returns>Read-only view of the bytes in this lob.</returns>
        /// <exception cref="NullValueException">If this lob is null.</exception>
        public ReadOnlySpan<byte> Bytes()
        {
            ThrowIfNull();
            Debug.Assert(ByteBuffer != null);
            return ByteBuffer.AsSpan();
        }

        /// <summary>
        /// Copy the bytes from the buffer to this lob.
        /// </summary>
        /// <param name="buffer">Byte buffer</param>
        public void SetBytes(ReadOnlySpan<byte> buffer)
        {
            ThrowIfLocked();
            //this is bad but this operation is pretty non-common.
            Array.Resize(ref ByteBuffer, buffer.Length);
            buffer.CopyTo(ByteBuffer);
            NullFlagOn(false);
        }

        public override void MakeNull()
        {
            base.MakeNull();
            ByteBuffer = null;
        }
    }
}
