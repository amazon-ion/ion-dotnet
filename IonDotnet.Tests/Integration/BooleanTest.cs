using IonDotnet.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Integration
{
    [TestClass]
    public class BooleanTest : IntegrationTestBase
    {
        [TestMethod]
        [DataRow(InputStyle.MemoryStream)]
        [DataRow(InputStyle.FileStream)]
        [DataRow(InputStyle.Text)]
        [DataRow(InputStyle.NoSeekStream)]
        public void BooleanText(InputStyle inputStyle)
        {
            void assertReader(IIonReader reader)
            {
                Assert.AreEqual(IonType.Bool, reader.MoveNext());
                Assert.AreEqual(true, reader.BoolValue());
                Assert.AreEqual(IonType.Bool, reader.MoveNext());
                Assert.AreEqual(false, reader.BoolValue());
            }

            void writerFunc(IIonWriter writer)
            {
                writer.WriteBool(true);
                writer.WriteBool(false);
                writer.Finish();
            }

            var file = DirStructure.IonTestFile("good/booleans.ion");
            var r = ReaderFromFile(file, inputStyle);
            assertReader(r);

            AssertReaderWriter(assertReader, writerFunc);
        }

        [DataRow(InputStyle.MemoryStream)]
        [DataRow(InputStyle.FileStream)]
        [DataRow(InputStyle.NoSeekStream)]
        [TestMethod]
        public void NullBoolBinary(InputStyle inputStyle)
        {
            void assertReader(IIonReader reader)
            {
                Assert.AreEqual(IonType.Bool, reader.MoveNext());
                Assert.IsTrue(reader.CurrentIsNull);
            }

            void writerFunc(IIonWriter writer)
            {
                writer.WriteNull(IonType.Bool);
                writer.Finish();
            }

            var file = DirStructure.IonTestFile("good/nullBool.10n");
            var r = ReaderFromFile(file, inputStyle);
            assertReader(r);

            AssertReaderWriter(assertReader, writerFunc);
        }
    }
}
