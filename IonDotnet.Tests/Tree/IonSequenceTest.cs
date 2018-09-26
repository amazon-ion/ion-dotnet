using System.Diagnostics;
using System.Linq;
using IonDotnet.Tree;
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
    }
}
