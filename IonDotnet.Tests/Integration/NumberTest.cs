using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
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
                writer.Finish();
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
                Assert.AreEqual(18446744073709551615m, reader.DecimalValue().ToDecimal());

                Assert.AreEqual(IonType.Decimal, reader.MoveNext());
                Assert.AreEqual(-18446744073709551615.0m, reader.DecimalValue().ToDecimal());

                Assert.AreEqual(IonType.Decimal, reader.MoveNext());
                Assert.AreEqual(18446744073709551616.0m, reader.DecimalValue().ToDecimal());

                Assert.AreEqual(IonType.Decimal, reader.MoveNext());
                Assert.AreEqual(-18446744073709551616.0m, reader.DecimalValue().ToDecimal());
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
                Assert.AreEqual(-1.28m, reader.DecimalValue().ToDecimal());
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
                Assert.AreEqual(1.23m, reader.DecimalValue().ToDecimal());
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
                    Assert.AreEqual(num, reader.DecimalValue().ToDecimal());
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

        [TestMethod]
        public void FloatDblMin()
        {
            var file = DirStructure.IonTestFile("good/floatDblMin.ion");
            var floats = new[]
            {
                2.2250738585072012e-308,
                0.00022250738585072012e-304,
                2.225073858507201200000e-308,
                2.2250738585072012e-00308,
                2.2250738585072012997800001e-308
            };

            void assertReader(IIonReader reader)
            {
                foreach (var f in floats)
                {
                    Assert.AreEqual(IonType.Float, reader.MoveNext());
                    ReaderTestCommon.AssertFloatEqual(f, reader.DoubleValue());
                }

                Assert.AreEqual(IonType.None, reader.MoveNext());
            }

            void writerFunc(IIonWriter writer)
            {
                foreach (var f in floats)
                {
                    writer.WriteFloat(f);
                }

                writer.Finish();
            }

            var r = ReaderFromFile(file, InputStyle.FileStream);
            assertReader(r);

            AssertReaderWriter(assertReader, writerFunc);
        }

        [TestMethod]
        public void FloatSpecials()
        {
            var file = DirStructure.IonTestFile("good/floatSpecials.ion");

            void assertReader(IIonReader reader)
            {
                Assert.AreEqual(IonType.List, reader.MoveNext());
                reader.StepIn();

                Assert.AreEqual(IonType.Float, reader.MoveNext());
                Assert.IsTrue(double.IsNaN(reader.DoubleValue()));

                Assert.AreEqual(IonType.Float, reader.MoveNext());
                Assert.IsTrue(double.IsPositiveInfinity(reader.DoubleValue()));

                Assert.AreEqual(IonType.Float, reader.MoveNext());
                Assert.IsTrue(double.IsNegativeInfinity(reader.DoubleValue()));

                Assert.AreEqual(IonType.None, reader.MoveNext());
            }

            void writerFunc(IIonWriter writer)
            {
                writer.StepIn(IonType.List);

                writer.WriteFloat(double.NaN);
                writer.WriteFloat(double.PositiveInfinity);
                writer.WriteFloat(double.NegativeInfinity);

                writer.StepOut();
                writer.Finish();
            }

            var r = ReaderFromFile(file, InputStyle.FileStream);
            assertReader(r);

            AssertReaderWriter(assertReader, writerFunc);
        }

        [TestMethod]
        public void FloatWithTerminatingEof()
        {
            var file = DirStructure.IonTestFile("good/floatWithTerminatingEof.ion");
            var r = ReaderFromFile(file, InputStyle.FileStream);
            Assert.AreEqual(IonType.Float, r.MoveNext());
            ReaderTestCommon.AssertFloatEqual(12.3, r.DoubleValue());

            Assert.AreEqual(IonType.None, r.MoveNext());
        }

        [TestMethod]
        public void Float_zeros()
        {
            var file = DirStructure.IonTestFile("good/float_zeros.ion");
            var reader = ReaderFromFile(file, InputStyle.FileStream);
            while (reader.MoveNext() != IonType.None)
            {
                Assert.AreEqual(IonType.Float, reader.CurrentType);
                Assert.AreEqual(0d, reader.DoubleValue());
            }
        }

        [TestMethod]
        [DataRow("good/intBigSize256.10n")]
        [DataRow("good/intBigSize256.ion")]
        public void IntBigSize256(string fileName)
        {
            var file = DirStructure.IonTestFile(fileName);
            var r = ReaderFromFile(file, InputStyle.FileStream);
            BigInteger b;

            void assertReader(IIonReader reader)
            {
                Assert.AreEqual(IonType.Int, reader.MoveNext());
                Assert.AreEqual(IntegerSize.BigInteger, reader.GetIntegerSize());
                b = reader.BigIntegerValue();
            }

            void writerFunc(IIonWriter writer)
            {
                writer.WriteInt(b);
                writer.Finish();
            }

            assertReader(r);
            AssertReaderWriter(assertReader, writerFunc);
        }

        [DataRow("good/intBigSize512.ion")]
        [TestMethod]
        public void IntBigSize512(string fileName)
        {
            var file = DirStructure.IonTestFile(fileName);
            var r = ReaderFromFile(file, InputStyle.FileStream);
            BigInteger b;

            void assertReader(IIonReader reader)
            {
                Assert.AreEqual(IonType.Int, reader.MoveNext());
                Assert.AreEqual(IntegerSize.BigInteger, reader.GetIntegerSize());
                b = reader.BigIntegerValue();
            }

            void writerFunc(IIonWriter writer)
            {
                writer.WriteInt(b);
                writer.Finish();
            }

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
