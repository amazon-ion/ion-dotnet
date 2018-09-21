using IonDotnet.Internals;

namespace IonDotnet.Tree
{
    public sealed class IonBlob : IonLob
    {
        internal override void WriteBodyTo(IPrivateWriter writer)
        {
            writer.WriteBlob(Bytes());
        }

        public override IonType Type => IonType.Blob;
    }
}
