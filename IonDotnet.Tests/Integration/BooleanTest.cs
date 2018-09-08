using System.IO;
using IonDotnet.Systems;
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
        [DataRow(InputStyle.NoSeek)]
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

            //bin
            using (var s = new MemoryStream())
            {
                var binWriter = IonBinaryWriterBuilder.Build(s);
                writerFunc(binWriter);
                s.Seek(0, SeekOrigin.Begin);
                var binReader = IonReaderBuilder.Build(s);
                assertReader(binReader);
            }

            //text
            var sw = new StringWriter();
            var textWriter = IonTextWriterBuilder.Build(sw);
            writerFunc(textWriter);
            var textReader = IonReaderBuilder.Build(sw.ToString());
            assertReader(textReader);
        }

        [DataRow(InputStyle.MemoryStream)]
        [DataRow(InputStyle.FileStream)]
        [DataRow(InputStyle.NoSeek)]
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
