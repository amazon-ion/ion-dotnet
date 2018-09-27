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
            Assert.AreEqual(0, v.Count);
            var c1 = MakeMutableValue();

            Assert.IsFalse(v.ContainsField(field));
            Assert.IsFalse(v.Contains(c1));
            v[field] = c1;
            Assert.AreEqual(1, v.Count);
            Assert.IsTrue(v.ContainsField(field));
            Assert.AreEqual(c1.Container, v);
            Assert.IsTrue(v.Contains(c1));
            Assert.AreEqual(c1, v[field]);

            var c2 = MakeMutableValue();
            v[field] = c2;
            Assert.AreEqual(1, v.Count);
            Assert.IsFalse(v.Contains(c1));
            Assert.IsNull(c1.Container);
            Assert.IsTrue(v.Contains(c2));
            Assert.IsTrue(v.ContainsField(field));
            Assert.AreEqual(c2, v[field]);
        }

        [TestMethod]
        public void RemoveField()
        {
            const string field = "field";
            var v = (IonStruct) MakeMutableValue();
            var c = MakeMutableValue();
            v[field] = c;
            var removed = v.RemoveField(field);

            Assert.IsTrue(removed);
            Assert.AreEqual(0, v.Count);
            Assert.IsNull(c.Container);
            Assert.IsFalse(v.Contains(c));
            Assert.IsFalse(v.RemoveField(field));
        }

        [TestMethod]
        public void RemoveItem()
        {
            const string field = "field";
            var v = (IonStruct) MakeMutableValue();
            var c = MakeMutableValue();
            v[field] = c;
            v.Remove(c);
            Assert.AreEqual(0, v.Count);
            Assert.IsNull(c.Container);
            Assert.IsFalse(v.Contains(c));
        }

        [TestMethod]
        public void StructEquivalence_True()
        {
            var s1 = BuildFlatStruct(1, 10);
            var s2 = BuildFlatStruct(1, 10);
            Assert.IsTrue(s1.IsEquivalentTo(s2));

            var n1 = IonStruct.NewNull();
            var n2 = IonStruct.NewNull();
            Assert.IsTrue(n1.IsEquivalentTo(n2));
        }

        [TestMethod]
        public void StructEquivalence_False()
        {
            var s1 = BuildFlatStruct(1, 10);
            var s2 = BuildFlatStruct(1, 9);
            var n = IonStruct.NewNull();

            Assert.IsFalse(s1.IsEquivalentTo(s2));
            Assert.IsFalse(s1.IsEquivalentTo(n));
            Assert.IsFalse(n.IsEquivalentTo(s1));
            s2["field10"] = new IonBool(true);
            Assert.IsFalse(s1.IsEquivalentTo(s2));

            s2.RemoveField("field10");
            s2["field10"] = new IonInt(10);
            Assert.IsTrue(s1.IsEquivalentTo(s2));
                
            //different field name
            s2.RemoveField("field10");
            s2["another"] = new IonInt(10);
            Assert.IsFalse(s1.IsEquivalentTo(s2));
        }

        private IonStruct BuildFlatStruct(int start, int count)
        {
            var str = (IonStruct) MakeMutableValue();
            for (var i = 0; i < count; i++)
            {
                str[$"field{start + i}"] = new IonInt(start + i);
            }

            return str;
        }
    }
}
