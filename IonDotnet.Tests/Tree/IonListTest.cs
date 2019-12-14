using IonDotnet.Tree;
using IonDotnet.Tree.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Tree
{
    [TestClass]
    public class IonListTest : IonSequenceTest
    {
        protected override IIonValue MakeMutableValue()
        {
            return new IonList();
        }

        internal override IonContainer MakeNullValue()
        {
            return IonList.NewNull();
        }

        internal override void DoAdd(IonContainer container, IonValue item)
        {
            var list = (IonList) container;
            list.Add(item);
        }
    }
}
