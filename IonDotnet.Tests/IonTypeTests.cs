using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests
{
    [TestClass]
    public class IonTypeTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            Console.WriteLine(IonType.Blob > IonType.List);
        }
    }
}
