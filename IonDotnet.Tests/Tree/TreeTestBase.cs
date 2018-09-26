using IonDotnet.Tree;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Tree
{
    public abstract class TreeTestBase
    {
        protected abstract IonValue MakeMutableValue();

        [DataRow(new string[0])]
        [DataRow(new[] {"a"})]
        [DataRow(new[] {"a", "b"})]
        [DataRow(new[] {"bool", "int"})]
        [TestMethod]
        public void AddAnnotations(string[] annotations)
        {
            var v = MakeMutableValue();
            foreach (var annotation in annotations)
            {
                v.AddTypeAnnotation(annotation);
            }

            foreach (var annotation in annotations)
            {
                Assert.IsTrue(v.HasAnnotation(annotation));
            }
        }
    }
}
