using System.Text;
using IonDotnet.Internals.Text;
using IonDotnet.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Internals
{
    [TestClass]
    public class TestBool
    {
        [TestMethod]
        public void TestBasicBool()
        {
            byte[] values = DirStructure.IonTestFileAsBytes("good/booleans.ion");
            string str = Encoding.UTF8.GetString(values);
            
            var reader = new UserTextReader(str);
            reader.MoveNext();
            Assert.AreEqual(true, reader.BoolValue());
            var a = reader.MoveNext();
            Assert.AreEqual(false, reader.BoolValue());
        }
    }
}