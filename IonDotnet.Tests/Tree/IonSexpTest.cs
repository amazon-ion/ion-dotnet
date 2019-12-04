using IonDotnet.Tree.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Tree
{
    [TestClass]
    public class IonSexpTest : IonSequenceTest
    {
        protected override IonValue MakeMutableValue()
        {
            return new IonSexp();
        }

        protected override IonContainer MakeNullValue()
        {
            return IonSexp.NewNull();
        }

        protected override void DoAdd(IonContainer container, IonValue item)
        {
            container.Add(item);
        }
    }
}
