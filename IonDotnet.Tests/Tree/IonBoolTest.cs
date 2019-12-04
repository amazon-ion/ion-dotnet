using System;
using IonDotnet.Tree.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Tree
{
    [TestClass]
    public class IonBoolTest : TreeTestBase
    {
        protected override IonValue MakeMutableValue() => new IonBool(false);

        [TestMethod]
        public void Null()
        {
            var n = IonBool.NewNull();
            Assert.AreEqual(IonType.Bool, n.Type);
            Assert.IsTrue(n.IsNull);
            Assert.ThrowsException<NullValueException>(() => n.Value);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SimpleValue(bool value)
        {
            var v = new IonBool(value);
            Assert.AreEqual(IonType.Bool, v.Type);
            Assert.IsFalse(v.IsNull);
            Assert.AreEqual(value, v.Value);
            v.Value = !value;
            Assert.AreEqual(!value, v.Value);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataRow(null)]
        [TestMethod]
        public void SetReadOnly(bool? value)
        {
            var v = value is null ? IonBool.NewNull() : new IonBool(value.Value);
            Assert.IsFalse(v.IsReadOnly);
            v.MakeReadOnly();
            Assert.IsTrue(v.IsReadOnly);
            Assert.ThrowsException<InvalidOperationException>(() => v.Value = value != true);
            Assert.ThrowsException<InvalidOperationException>(() => v.AddTypeAnnotation("something"));
            Assert.ThrowsException<InvalidOperationException>(() => v.MakeNull());
        }


        [DataRow(true)]
        [DataRow(false)]
        [TestMethod]
        public void BooleanEquality(bool value)
        {
            var nullBool = IonBool.NewNull();
            var null2 = IonBool.NewNull();
            var v = new IonBool(value);
            var v2 = new IonBool(value);
            var ionInt = new IonInt(3);
            var vd = new IonBool(!value);

            Assert.IsFalse(v.IsEquivalentTo(nullBool));
            Assert.IsFalse(v.IsEquivalentTo(vd));
            Assert.IsFalse(nullBool.IsEquivalentTo(v));
            Assert.IsTrue(nullBool.IsEquivalentTo(IonBool.NewNull()));
            Assert.IsTrue(v.IsEquivalentTo(v2));
            Assert.IsFalse(v.IsEquivalentTo(ionInt));
            Assert.IsTrue(nullBool.IsEquivalentTo(null2));
        }
    }
}
