using System;
using IonDotnet.Tree.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Tree
{
    [TestClass]
    public class IonBlobTest : IonLobTest
    {
        protected override IonValue MakeMutableValue()
        {
            return new IonBlob(new byte[0]);
        }

        protected override IonLob MakeNullValue()
        {
            return IonBlob.NewNull();
        }

        protected override IonLob MakeWithBytes(ReadOnlySpan<byte> bytes)
        {
            return new IonBlob(bytes);
        }

        protected override IonType MainIonType => IonType.Blob;
    }
}
