using IonDotnet.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Integration
{
    [TestClass]
    public class BooleanTest : TestBase
    {
        [TestMethod]
        [DataRow(InputStyle.MemoryStream)]
        [DataRow(InputStyle.FileStream)]
        [DataRow(InputStyle.Text)]
        public void BooleanText(InputStyle inputStyle)
        {
            var file = DirStructure.IonTestFile("good/booleans.ion");
            var reader = ReaderFromFile(file, inputStyle);

            Assert.AreEqual(IonType.Bool, reader.MoveNext());
            Assert.AreEqual(true, reader.BoolValue());
            Assert.AreEqual(IonType.Bool, reader.MoveNext());
            Assert.AreEqual(false, reader.BoolValue());
        }

        [DataRow(InputStyle.MemoryStream)]
        [DataRow(InputStyle.FileStream)]
        [TestMethod]
        public void NullBoolBinary(InputStyle inputStyle)
        {
            var file = DirStructure.IonTestFile("good/nullBool.10n");
            var reader = ReaderFromFile(file, inputStyle);

            Assert.AreEqual(IonType.Bool, reader.MoveNext());
            Assert.IsTrue(reader.CurrentIsNull);
        }
    }
}
