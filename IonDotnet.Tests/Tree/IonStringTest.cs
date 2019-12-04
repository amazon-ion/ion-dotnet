using System;
using IonDotnet.Tree.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Tree
{
    [TestClass]
    public class IonStringTest : TreeTestBase
    {
        protected override IonValue MakeMutableValue() => new IonString("s");

        [TestMethod]
        public void Null()
        {
            var n = new IonString(null);
            Assert.AreEqual(IonType.String, n.Type);
            Assert.IsTrue(n.IsNull);
            Assert.IsNull(n.StringValue);
        }

        [TestMethod]
        [DataRow("", "abcd")]
        [DataRow("this is some string", "abcd")]
        [DataRow("this is some string", null)]
        public void SimpleValue(string value, string value2)
        {
            var v = new IonString(value);
            Assert.AreEqual(IonType.String, v.Type);
            Assert.IsFalse(v.IsNull);
            Assert.AreEqual(value, v.StringValue);

            v.StringValue = value2;
            Assert.AreEqual(value2, v.StringValue);
        }

        [DataRow(null)]
        [DataRow("")]
        [DataRow("some string")]
        [TestMethod]
        public void SetReadonly(string value)
        {
            var v = new IonString(value);
            Assert.IsFalse(v.IsReadOnly);
            v.MakeReadOnly();
            Assert.IsTrue(v.IsReadOnly);
            Assert.ThrowsException<InvalidOperationException>(() => v.StringValue = "something else");
            Assert.ThrowsException<InvalidOperationException>(() => v.StringValue = null);
            Assert.ThrowsException<InvalidOperationException>(() => v.AddTypeAnnotation("abc"));
            Assert.ThrowsException<InvalidOperationException>(() => v.MakeNull());
        }

        [DataRow(null)]
        [DataRow("")]
        [DataRow("some string")]
        [TestMethod]
        public void StringEquality(string value)
        {
            var v = new IonString(value);
            var v2 = new IonString(value);
            var n = new IonString(null);
            var ionInt = new IonInt(3);

            Assert.IsTrue(v.IsEquivalentTo(v2));
            if (value is null)
            {
                Assert.IsTrue(v.IsEquivalentTo(n));
            }
            else
            {
                Assert.IsFalse(v.IsEquivalentTo(n));
            }

            Assert.IsFalse(v.IsEquivalentTo(ionInt));
        }
    }
}
