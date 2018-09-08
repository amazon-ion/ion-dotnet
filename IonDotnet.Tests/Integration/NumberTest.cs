using IonDotnet.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Integration
{
    [TestClass]
    public class NumberTest : IntegrationTestBase
    {
        [TestMethod]
        [DataRow(InputStyle.MemoryStream)]
        [DataRow(InputStyle.FileStream)]
        [DataRow(InputStyle.Text)]
        [DataRow(InputStyle.NoSeekStream)]
        public void HexWithTerminatingEof(InputStyle inputStyle)
        {
            void assertReader(IIonReader reader)
            {
                Assert.AreEqual(IonType.Int, reader.MoveNext());
                Assert.AreEqual(3, reader.IntValue());
                Assert.AreEqual(IonType.None, reader.MoveNext());
            }

            void writerFunc(IIonWriter writer)
            {
                writer.WriteInt(3);
            }

            var file = DirStructure.IonTestFile("good/hexWithTerminatingEof.ion");
            var r = ReaderFromFile(file, inputStyle);
            assertReader(r);

            AssertReaderWriter(assertReader, writerFunc);
        }

        [TestMethod]
        [DataRow(InputStyle.MemoryStream)]
        [DataRow(InputStyle.FileStream)]
        [DataRow(InputStyle.Text)]
        [DataRow(InputStyle.NoSeekStream)]
        public void Decimal64BitBoundary(InputStyle inputStyle)
        {
            void assertReader(IIonReader reader)
            {
                Assert.AreEqual(IonType.Decimal, reader.MoveNext());
                Assert.AreEqual(18446744073709551615m, reader.DecimalValue());
                
                Assert.AreEqual(IonType.Decimal, reader.MoveNext());
                Assert.AreEqual(-18446744073709551615m, reader.DecimalValue());
                
                Assert.AreEqual(IonType.Decimal, reader.MoveNext());
                Assert.AreEqual(18446744073709551616m, reader.DecimalValue());
                
                Assert.AreEqual(IonType.Decimal, reader.MoveNext());
                Assert.AreEqual(-18446744073709551616m, reader.DecimalValue());
            }

            void writerFunc(IIonWriter writer)
            {
                writer.WriteDecimal(18446744073709551615m);
                writer.WriteDecimal(-18446744073709551615m);
                writer.WriteDecimal(18446744073709551616m);
                writer.WriteDecimal(-18446744073709551616m);
            }

            var file = DirStructure.IonTestFile("good/decimal64BitBoundary.ion");
            var r = ReaderFromFile(file, inputStyle);
            assertReader(r);

            AssertReaderWriter(assertReader, writerFunc);
        }
    }
}
