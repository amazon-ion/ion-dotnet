using IonDotnet.Tree.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Tree
{
    [TestClass]
    internal class IonListTest : IonSequenceTest
    {
        internal override IonValue MakeMutableValue()
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
