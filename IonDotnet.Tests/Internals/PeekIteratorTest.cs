using System;
using IonDotnet.Internals;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Internals
{
    [TestClass]
    public class PeekIteratorTest
    {
        [TestMethod]
        [DataRow(new[] {"0", "1", "2"})]
        [DataRow(new[] {"0", null})]
        [DataRow(new string[] {null, null})]
        [DataRow(new string[0])]
        [DataRow(new[] {null, "1", "2"})]
        public void HasNext(string[] array)
        {
            var enumerator = new PeekIterator<string>(array);
            for (var i = 0; i <= array.Length; i++)
            {
                Assert.AreEqual(array.Length - i > 0, enumerator.HasNext());
                if (i < array.Length)
                {
                    enumerator.Next();
                }
            }
        }

        [TestMethod]
        [DataRow(new[] {"0", "1", "2"})]
        [DataRow(new[] {"0", null})]
        [DataRow(new string[] {null, null})]
        [DataRow(new string[0])]
        [DataRow(new[] {null, "1", "2"})]
        public void MoveNext(string[] array)
        {
            var enumerator = new PeekIterator<string>(array);
            foreach (var t in array)
            {
                Assert.AreEqual(t, enumerator.Next());
            }
        }

        [TestMethod]
        [DataRow(new[] {"0", "1", "2"})]
        [DataRow(new[] {"0", null})]
        [DataRow(new string[] {null, null})]
        [DataRow(new string[0])]
        [DataRow(new[] {null, "1", "2"})]
        public void MovePastRange_ThrowsException(string[] array)
        {
            var enumerator = new PeekIterator<string>(array);
            foreach (var unused in array)
            {
                enumerator.Next();
            }

            Assert.ThrowsException<InvalidOperationException>(() => enumerator.Next());
        }
    }
}
