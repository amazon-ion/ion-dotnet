using System;
using System.Linq;
using System.Text;
using IonDotnet.Tree.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Tree
{
    public abstract class IonLobTest : TreeTestBase
    {
        protected abstract IonLob MakeNullValue();

        protected abstract IonLob MakeWithBytes(ReadOnlySpan<byte> bytes);

        protected abstract IonType MainIonType { get; }

        [TestMethod]
        public void Null()
        {
            var n = MakeNullValue();
            Assert.IsTrue(n.IsNull);
            Assert.AreEqual(MainIonType, n.Type);
            Assert.ThrowsException<NullValueException>(() => n.Bytes());
            if (n is IonClob nclob)
            {
                Assert.ThrowsException<NullValueException>(() => nclob.NewReader(Encoding.UTF8));
            }
        }

        [TestMethod]
        public void MakeWithBytes()
        {
            var bytes = Enumerable.Repeat<byte>(1, 10).ToArray();
            var v = MakeWithBytes(bytes);
            Assert.AreEqual(MainIonType, v.Type);
            Assert.IsFalse(v.IsNull);
            Assert.IsTrue(v.Bytes().SequenceEqual(bytes));
            bytes[0] = 2;
            Assert.IsFalse(v.Bytes().SequenceEqual(bytes));

            //set bytes
            var bytes2 = Enumerable.Repeat<byte>(1, 20).ToArray();
            v.SetBytes(bytes2);
            Assert.IsTrue(v.Bytes().SequenceEqual(bytes2));
        }

        [TestMethod]
        public void ReadOnly()
        {
            var v = (IonLob) MakeMutableValue();
            Assert.IsFalse(v.IsReadOnly);
            v.MakeReadOnly();
            Assert.IsTrue(v.IsReadOnly);
            Assert.ThrowsException<InvalidOperationException>(() => v.MakeNull());
            Assert.ThrowsException<InvalidOperationException>(() => v.SetBytes(new byte[0]));
        }
    }
}
