using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Internals
{
    [TestClass]
    public class ImportLocationTest
    {
        [TestMethod]
        public void Init_SidAndTextUnknown()
        {
            var location = new ImportLocation();
            Assert.AreEqual(null, location.ImportName);
            Assert.AreEqual(0, location.Sid);
        }

        [TestMethod]
        [DataRow("text1", 123, "text1", 123)]
        [DataRow("text2", 456, "text2", 456)]
        public void Bool_EqualsOperator(string text1, int sid1, string text2, int sid2)
        {
            var location1 = new ImportLocation(text1, sid1);
            var location2 = new ImportLocation(text2, sid2);
            Assert.IsTrue(location1 == location2);
        }

        [TestMethod]
        [DataRow("text1", 123, "text1", 456)]
        [DataRow("text2", 456, "text3", 456)]
        public void Bool_NotEqualsOperator(string text1, int sid1, string text2, int sid2)
        {
            var location1 = new ImportLocation(text1, sid1);
            var location2 = new ImportLocation(text2, sid2);
            Assert.IsTrue(location1 != location2);
        }

        [TestMethod]
        public void EqualsMethod()
        {
            var location = CreateSampleToken();
            var equalLocation = new ImportLocation("yo", 30);
            var unEqualLocation = new ImportLocation("yo", 31);
            Assert.IsTrue(location.Equals(equalLocation));
            Assert.IsFalse(location.Equals(unEqualLocation));
        }

        private static ImportLocation CreateSampleToken() => new ImportLocation("yo", 30);
    }
}
