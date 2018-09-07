using IonDotnet.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Integration
{
    [TestClass]
    public class BooleanTest
    {
        [TestMethod]
        [DataRow(InputStyle.MemoryStream)]
        [DataRow(InputStyle.Text)]
        public void BooleanText(InputStyle inputStyle)
        {
            var file = DirStructure.IonTestFile("good/booleans.ion");
            var reader = TestReader.FromFile(file, inputStyle);

            Assert.AreEqual(IonType.Bool, reader.MoveNext());
            Assert.AreEqual(true, reader.BoolValue());
            Assert.AreEqual(IonType.Bool, reader.MoveNext());
            Assert.AreEqual(false, reader.BoolValue());
        }

        [DataRow(InputStyle.MemoryStream)]
        [TestMethod]
        public void NullBoolBinary(InputStyle inputStyle)
        {
            var file = DirStructure.IonTestFile("good/nullBool.10n");
            var reader = TestReader.FromFile(file, inputStyle);

            Assert.AreEqual(IonType.Bool, reader.MoveNext());
            Assert.IsTrue(reader.CurrentIsNull);
        }
    }
}
