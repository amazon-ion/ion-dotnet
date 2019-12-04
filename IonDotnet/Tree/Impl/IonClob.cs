using System;
using System.IO;
using System.Text;
using IonDotnet.Internals;

namespace IonDotnet.Tree.Impl
{
    /// <inheritdoc />
    /// <summary>
    /// An Ion clob value.
    /// </summary>
    public sealed class IonClob : IonLob, IIonClob
    {
        public IonClob(ReadOnlySpan<byte> bytes) : base(bytes)
        {
        }

        private IonClob()
        {
        }

        /// <summary>
        /// Construct a new null.clob value.
        /// </summary>
        public static IonClob NewNull() => new IonClob();

        public override bool IsEquivalentTo(IonValue other)
        {
            if (!base.IsEquivalentTo(other))
                return false;
            
            if (!(other is IonClob otherClob))
                return false;
            if (NullFlagOn())
                return otherClob.IsNull;
            return !otherClob.IsNull && otherClob.Bytes().SequenceEqual(Bytes());
        }

        internal override void WriteBodyTo(IPrivateWriter writer)
        {
            if (NullFlagOn())
            {
                writer.WriteNull(IonType.Clob);
                return;
            }

            writer.WriteClob(Bytes());
        }

        public override IonType Type => IonType.Clob;

        /// <summary>
        /// Returns a new <see cref="StreamReader"/> to read the content of this clob.
        /// </summary>
        /// <param name="encoding">Encoding to use.</param>
        public StreamReader NewReader(Encoding encoding)
        {
            ThrowIfNull();
            return new StreamReader(new MemoryStream(ByteBuffer), encoding);
        }
    }
}
