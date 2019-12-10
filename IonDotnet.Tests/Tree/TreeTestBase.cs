using System.Linq;
using IonDotnet.Tree.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Tree
{
    internal abstract class TreeTestBase
    {
        internal abstract IonValue MakeMutableValue();

        [DataRow(new string[0])]
        [DataRow(new[] {"a"})]
        [DataRow(new[] {"a", "b"})]
        [DataRow(new[] {"bool", "int"})]
        [TestMethod]
        public void AddAnnotations(string[] annotations)
        {
            var v = MakeMutableValue();
            Assert.AreEqual(0, v.GetTypeAnnotations().Count);

            foreach (var annotation in annotations)
            {
                v.AddTypeAnnotation(annotation);
            }

            Assert.AreEqual(annotations.Length, v.GetTypeAnnotations().Count);

            var annotReturns = v.GetTypeAnnotations();
            foreach (var annotation in annotations)
            {
                Assert.IsTrue(v.HasAnnotation(annotation));
                Assert.IsTrue(annotReturns.Any(a => a.Text == annotation));
            }
        }
    }
}
