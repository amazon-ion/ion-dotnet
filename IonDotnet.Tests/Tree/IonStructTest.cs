using IonDotnet.Tree;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Tree
{
    [TestClass]
    public class IonStructTest : IonContainerTest
    {
        private static int _serial = 1;

        protected override IonValue MakeMutableValue()
        {
            return new IonStruct();
        }

        protected override IonContainer MakeNullValue()
        {
            return IonStruct.NewNull();
        }

        protected override void DoAdd(IonContainer container, IonValue item)
        {
            var fieldName = $"Field{_serial++}";
            var v = (IonStruct) container;
            v[fieldName] = item;
        }

        [TestMethod]
        public void Add_Replace()
        {
            const string field = "field";

            var v = (IonStruct) MakeMutableValue();
            var c1 = MakeMutableValue();

            Assert.IsFalse(v.ContainsField(field));
            Assert.IsFalse(v.Contains(c1));
            v[field] = c1;
            Assert.IsTrue(v.ContainsField(field));
            Assert.AreEqual(c1.Container, v);
            Assert.IsTrue(v.Contains(c1));
            Assert.AreEqual(c1, v[field]);

            var c2 = MakeMutableValue();
            v[field] = c2;
            Assert.IsFalse(v.Contains(c1));
            Assert.IsNull(c1.Container);
            Assert.IsTrue(v.Contains(c2));
            Assert.IsTrue(v.ContainsField(field));
            Assert.AreEqual(c2, v[field]);
        }
    }
}
