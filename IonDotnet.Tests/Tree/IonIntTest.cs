using System;
using System.Numerics;
using IonDotnet.Tree.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Tree
{
    [TestClass]
    public class IonIntTest : TreeTestBase
    {
        protected override IonValue MakeMutableValue() => new IonInt(0);

        [TestMethod]
        public void Null()
        {
            var n = IonInt.NewNull();
            Assert.AreEqual(IonType.Int, n.Type);
            Assert.IsTrue(n.IsNull);
            Assert.AreEqual(IntegerSize.Unknown, n.IntegerSize);
            Assert.ThrowsException<NullValueException>(() => n.IntValue);
            Assert.ThrowsException<NullValueException>(() => n.LongValue);
            Assert.ThrowsException<NullValueException>(() => n.BigIntegerValue);
        }

        [DataRow(0)]
        [DataRow(1)]
        [DataRow(int.MinValue)]
        [DataRow(int.MaxValue)]
        [TestMethod]
        public void SimpleIntValue(int value)
        {
            void AssertEqual(IonInt iv, int expected)
            {
                Assert.AreEqual(IntegerSize.Int, iv.IntegerSize);
                Assert.AreEqual(expected, iv.IntValue);
                Assert.AreEqual(expected, iv.LongValue);
                Assert.AreEqual(expected, iv.BigIntegerValue);
            }

            var v = new IonInt(value);
            Assert.IsFalse(v.IsNull);
            AssertEqual(v, value);

            v.IntValue = 10;
            AssertEqual(v, 10);
        }

        [DataRow((long) int.MinValue - 10)]
        [DataRow((long) int.MaxValue + 10)]
        [DataRow(long.MinValue)]
        [DataRow(long.MaxValue)]
        [TestMethod]
        public void SimpleLongValue(long value)
        {
            void AssertEqual(IonInt iv, long expected)
            {
                Assert.AreEqual(IntegerSize.Long, iv.IntegerSize);
                Assert.AreEqual(expected, iv.LongValue);
                Assert.AreEqual(expected, iv.BigIntegerValue);
            }

            var v = new IonInt(value);
            Assert.IsFalse(v.IsNull);
            AssertEqual(v, value);

            v.LongValue = long.MaxValue - 1000;
            AssertEqual(v, long.MaxValue - 1000);
        }

        [TestMethod]
        public void SimpleBigIntValue()
        {
            void AssertEqual(IonInt iv, BigInteger expected)
            {
                Assert.AreEqual(IntegerSize.BigInteger, iv.IntegerSize);
                Assert.AreEqual(expected, iv.BigIntegerValue);
            }

            var b1 = new BigInteger(long.MaxValue);
            b1 = b1 + 1000;
            var v = new IonInt(b1);
            AssertEqual(v, b1);
            var b2 = b1 + 1000;
            v.BigIntegerValue = b2;
            AssertEqual(v, b2);
        }

        [TestMethod]
        public void SetReadOnly()
        {
            var v = new IonInt(0);
            Assert.IsFalse(v.IsReadOnly);
            v.MakeReadOnly();
            Assert.IsTrue(v.IsReadOnly);

            Assert.ThrowsException<InvalidOperationException>(() => v.IntValue = 10);
            Assert.ThrowsException<InvalidOperationException>(() => v.AddTypeAnnotation("something"));
            Assert.ThrowsException<InvalidOperationException>(() => v.MakeNull());
        }

        [DataRow(0)]
        [DataRow(int.MaxValue)]
        [DataRow(-3)]
        [TestMethod]
        public void IntEquality(int value)
        {
            var v = new IonInt(value);
            var v2 = new IonInt(value);
            var vd = new IonInt(value + (value > 0 ? -5 : 5));
            var n = IonInt.NewNull();
            var vb = new IonInt(new BigInteger(value));

            void AssertEquals()
            {
                Assert.IsTrue(v.IsEquivalentTo(vb));
                Assert.IsTrue(v.IsEquivalentTo(v2));
                Assert.IsFalse(v.IsEquivalentTo(n));
                Assert.IsFalse(v.IsEquivalentTo(vd));
            }

            AssertEquals();

            v = new IonInt((long) int.MaxValue + 10);
            v2 = new IonInt((long) int.MaxValue + 10);
            vb = new IonInt(new BigInteger((long) int.MaxValue + 10));
            AssertEquals();
        }
    }
}
