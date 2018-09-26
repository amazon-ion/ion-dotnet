using IonDotnet.Internals;

namespace IonDotnet.Tree
{
    public sealed class IonBlob : IonLob
    {
        public override bool Equals(IonValue other)
        {
            throw new System.NotImplementedException();
        }

        internal override void WriteBodyTo(IPrivateWriter writer)
        {
            writer.WriteBlob(Bytes());
        }

        public override IonType Type => IonType.Blob;
    }
}
