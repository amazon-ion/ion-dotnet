using System;
using System.Diagnostics;

namespace IonDotnet.Tree
{
    public abstract class IonLob : IonValue
    {
        private byte[] _bytes;

        protected IonLob() : base(true)
        {
        }

        protected IonLob(Span<byte> bytes) : base(false)
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
            Debug.Assert(_bytes != null);
            return _bytes.AsSpan();
        }

        /// <summary>
        /// Copy the bytes from the buffer to this lob.
        /// </summary>
        /// <param name="buffer">Byte buffer</param>
        public void SetBytes(Span<byte> buffer)
        {
            //this is bad but this operation is pretty non-common.
            Array.Resize(ref _bytes, buffer.Length);
            buffer.CopyTo(_bytes);
            NullFlagOn(false);
        }

        public override bool IsNull
        {
            get => base.IsNull;
            set
            {
                base.IsNull = value;
                if (value)
                {
                    _bytes = null;
                }
            }
        }
    }
}
