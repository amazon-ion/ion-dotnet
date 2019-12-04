using System.Diagnostics;
using System.Linq;
using IonDotnet.Tree.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Tree
{
    public abstract class IonSequenceTest : IonContainerTest
    {
        [TestMethod]
        [DataRow(0, 1)]
        [DataRow(1, 5)]
        [DataRow(6, 10)]
        [DataRow(99, 100)]
        public void IndexOf(int idx, int count)
        {
            Debug.Assert(count > idx);
            var v = (IonSequence) MakeMutableValue();
            IonValue r = null;
            for (var i = 0; i < count; i++)
            {
                var c = MakeMutableValue();
                if (i == idx)
                {
                    r = c;
                }

                DoAdd(v, c);
            }

            Assert.AreEqual(idx, v.IndexOf(r));
        }

        [TestMethod]
        [DataRow(0, 1)]
        [DataRow(1, 5)]
        [DataRow(6, 10)]
        [DataRow(99, 100)]
        public void Insert(int idx, int count)
        {
            Debug.Assert(count > idx);
            var v = (IonSequence) MakeMutableValue();
            var r = MakeMutableValue();
            for (var i = 0; i < count; i++)
            {
                var c = MakeMutableValue();
                DoAdd(v, c);
            }

            v.Insert(idx, r);
            Assert.AreEqual(v, r.Container);
            Assert.AreEqual(count + 1, v.Count);
            Assert.AreEqual(idx, v.IndexOf(r));
        }

        [TestMethod]
        [DataRow(0, 1)]
        [DataRow(1, 5)]
        [DataRow(6, 10)]
        [DataRow(99, 100)]
        public void RemoveAt(int idx, int count)
        {
            Debug.Assert(count > idx);
            var v = (IonSequence) MakeMutableValue();
            for (var i = 0; i < count; i++)
            {
                var c = MakeMutableValue();
                DoAdd(v, c);
            }

            var r = v[idx];
            v.RemoveAt(idx);
            Assert.AreEqual(count - 1, v.Count);
            Assert.IsNull(r.Container);
        }

        [TestMethod]
        public void SequenceEquality_True()
        {
            var s1 = BuildFlatSequence(1, 10);
            var s2 = BuildFlatSequence(1, 10);
            Assert.IsTrue(s1.IsEquivalentTo(s2));

            var n1 = MakeNullValue();
            var n2 = MakeNullValue();
            Assert.IsTrue(n1.IsEquivalentTo(n2));
        }

        [TestMethod]
        public void SequenceEquality_False()
        {
            var s1 = BuildFlatSequence(1, 10);
            var s2 = BuildFlatSequence(1, 9);
            Assert.IsFalse(s1.IsEquivalentTo(s2));
            s2.Add(new IonBool(true));
            Assert.IsFalse(s1.IsEquivalentTo(s2));

            var n = MakeNullValue();
            Assert.IsFalse(s1.IsEquivalentTo(n));
            Assert.IsFalse(n.IsEquivalentTo(s1));
        }

        private IonSequence BuildFlatSequence(int start, int count)
        {
            var s = (IonSequence) MakeMutableValue();
            for (var i = 0; i < count; i++)
            {
                s.Add(new IonInt(start + i));
            }

            return s;
        }
    }
}
