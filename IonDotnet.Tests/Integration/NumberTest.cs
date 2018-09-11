using System;
using System.Collections.Generic;
using System.IO;
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
                Assert.AreEqual(-18446744073709551615.0m, reader.DecimalValue());

                Assert.AreEqual(IonType.Decimal, reader.MoveNext());
                Assert.AreEqual(18446744073709551616.0m, reader.DecimalValue());

                Assert.AreEqual(IonType.Decimal, reader.MoveNext());
                Assert.AreEqual(-18446744073709551616.0m, reader.DecimalValue());
            }

            void writerFunc(IIonWriter writer)
            {
                writer.WriteDecimal(18446744073709551615m);
                writer.WriteDecimal(-18446744073709551615m);
                writer.WriteDecimal(18446744073709551616m);
                writer.WriteDecimal(-18446744073709551616m);
                writer.Finish();
            }

            var file = DirStructure.IonTestFile("good/decimal64BitBoundary.ion");
            var r = ReaderFromFile(file, inputStyle);
            assertReader(r);

            AssertReaderWriter(assertReader, writerFunc);
        }

        [TestMethod]
        [DataRow(InputStyle.MemoryStream)]
        [DataRow(InputStyle.FileStream)]
        [DataRow(InputStyle.Text)]
        [DataRow(InputStyle.NoSeekStream)]
        public void DecimalNegativeOneDotTwoEight(InputStyle inputStyle)
        {
            void assertReader(IIonReader reader)
            {
                Assert.AreEqual(IonType.Decimal, reader.MoveNext());
                Assert.AreEqual(-1.28m, reader.DecimalValue());
            }

            void writerFunc(IIonWriter writer)
            {
                writer.WriteDecimal(-1.28m);
                writer.Finish();
            }

            var file = DirStructure.IonTestFile("good/decimalNegativeOneDotTwoEight.ion");
            var r = ReaderFromFile(file, inputStyle);
            assertReader(r);

            AssertReaderWriter(assertReader, writerFunc);
        }

        [TestMethod]
        [DataRow(InputStyle.MemoryStream)]
        [DataRow(InputStyle.FileStream)]
        [DataRow(InputStyle.Text)]
        [DataRow(InputStyle.NoSeekStream)]
        public void DecimalWithTerminatingEof(InputStyle inputStyle)
        {
            void assertReader(IIonReader reader)
            {
                Assert.AreEqual(IonType.Decimal, reader.MoveNext());
                Assert.AreEqual(1.23m, reader.DecimalValue());
            }

            void writerFunc(IIonWriter writer)
            {
                writer.WriteDecimal(1.23m);
                writer.Finish();
            }

            var file = DirStructure.IonTestFile("good/decimalWithTerminatingEof.ion");
            var r = ReaderFromFile(file, inputStyle);
            assertReader(r);

            AssertReaderWriter(assertReader, writerFunc);
        }

        [TestMethod]
        [DataRow(InputStyle.MemoryStream)]
        [DataRow(InputStyle.FileStream)]
        [DataRow(InputStyle.Text)]
        [DataRow(InputStyle.NoSeekStream)]
        public void Decimal_e_values(InputStyle inputStyle)
        {
            var file = DirStructure.IonTestFile("good/decimal_e_values.ion");
            var nums = new List<decimal>();
            using (var fileStream = file.OpenRead())
            {
                using (var streamReader = new StreamReader(fileStream))
                {
                    string line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        nums.Add(ParseDecimal(line));
                    }
                }
            }

            void assertReader(IIonReader reader)
            {
                foreach (var num in nums)
                {
                    Assert.AreEqual(IonType.Decimal, reader.MoveNext());
                    Assert.AreEqual(num, reader.DecimalValue());
                }
            }

            void writerFunc(IIonWriter writer)
            {
                foreach (var num in nums)
                {
                    writer.WriteDecimal(num);
                }

                writer.Finish();
            }

            var r = ReaderFromFile(file, inputStyle);
            assertReader(r);

            AssertReaderWriter(assertReader, writerFunc);
        }

        [TestMethod]
        [DataRow(InputStyle.MemoryStream)]
        [DataRow(InputStyle.FileStream)]
        [DataRow(InputStyle.Text)]
        [DataRow(InputStyle.NoSeekStream)]
        public void Float_values(InputStyle inputStyle)
        {
            var file = DirStructure.IonTestFile("good/float_values.ion");
            var nums = new List<double>();
            using (var fileStream = file.OpenRead())
            {
                using (var streamReader = new StreamReader(fileStream))
                {
                    string line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        nums.Add(double.Parse(line));
                    }
                }
            }

            void assertReader(IIonReader reader)
            {
                foreach (var num in nums)
                {
                    var type = reader.MoveNext();
                    Assert.AreEqual(IonType.Float, type);
                    Assert.AreEqual(num, reader.DoubleValue());
                }
            }

            void writerFunc(IIonWriter writer)
            {
                foreach (var num in nums)
                {
                    writer.WriteFloat(num);
                }

                writer.Finish();
            }

            var r = ReaderFromFile(file, inputStyle);
            assertReader(r);

            AssertReaderWriter(assertReader, writerFunc);
        }

        private static decimal ParseDecimal(string s)
        {
            var idxOfD = s.IndexOf("d", StringComparison.OrdinalIgnoreCase);
            if (idxOfD < 0)
            {
                return decimal.Parse(s);
            }

            var co = decimal.Parse(s.Substring(0, idxOfD));
            var neg = idxOfD < s.Length - 1 && s[idxOfD + 1] == '-';
            if (neg)
            {
                idxOfD++;
            }

            var pow = idxOfD == s.Length - 1 ? 0 : int.Parse(s.Substring(idxOfD + 1));
            for (var i = 0; i < pow; i++)
            {
                co = neg ? co / 10 : co * 10;
            }

            return co;
        }
    }
}
