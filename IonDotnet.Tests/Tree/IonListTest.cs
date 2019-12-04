using IonDotnet.Tree.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Tree
{
    [TestClass]
    public class IonListTest : IonSequenceTest
    {
        protected override IonValue MakeMutableValue()
        {
            return new IonList();
        }

        protected override IonContainer MakeNullValue()
        {
            return IonList.NewNull();
        }

        protected override void DoAdd(IonContainer container, IonValue item)
        {
            var list = (IonList) container;
            list.Add(item);
        }
    }
}
