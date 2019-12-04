using System;
namespace IonDotnet.Tree
{
    public interface IIonLob : IIonValue
    {
        ReadOnlySpan<byte> Bytes();
        void SetBytes(ReadOnlySpan<byte> buffer);
        void MakeNull();
    }
}
