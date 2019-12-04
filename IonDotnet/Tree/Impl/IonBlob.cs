using System;
using IonDotnet.Internals;

namespace IonDotnet.Tree.Impl
{
    /// <inheritdoc />
    /// <summary>
    /// An Ion blob value.
    /// </summary>
    public sealed class IonBlob : IonLob, IIonBlob
    {
        public IonBlob(ReadOnlySpan<byte> bytes) : base(bytes)
        {
        }

        private IonBlob()
        {
        }

        /// <summary>
        /// Construct a new null.blob value.
        /// </summary>
        public static IonBlob NewNull() => new IonBlob();

        public override bool IsEquivalentTo(IonValue other)
        {
            if (!base.IsEquivalentTo(other))
                return false;

            var otherBlob = (IonBlob) other;

            if (NullFlagOn())
                return otherBlob.IsNull;
            return !otherBlob.IsNull && otherBlob.Bytes().SequenceEqual(Bytes());
        }

        internal override void WriteBodyTo(IPrivateWriter writer)
        {
            if (NullFlagOn())
            {
                writer.WriteNull(IonType.Blob);
                return;
            }

            writer.WriteBlob(Bytes());
        }

        public override IonType Type => IonType.Blob;
    }
}
