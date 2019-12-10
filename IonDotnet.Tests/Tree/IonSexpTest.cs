using IonDotnet.Tree.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Tree
{
    [TestClass]
    internal class IonSexpTest : IonSequenceTest
    {
        internal override IonValue MakeMutableValue()
        {
            return new IonSexp();
        }

        internal override IonContainer MakeNullValue()
        {
            return IonSexp.NewNull();
        }

        internal override void DoAdd(IonContainer container, IonValue item)
        {
            container.Add(item);
        }
    }
}
