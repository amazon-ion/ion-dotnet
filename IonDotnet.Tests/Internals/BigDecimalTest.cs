using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable ImpureMethodCallOnReadonlyValueField

namespace IonDotnet.Tests.Internals
{
    [TestClass]
    public class BigDecimalTest
    {
        public static IEnumerable<object[]> Decimals_DecimalPlaces()
        {
            return new List<object[]>
            {
                new object[] {0m, BigInteger.Zero, 0},
                new object[] {-1m * 0, BigInteger.Zero, 0},
                new object[] {45m, new BigInteger(45), 0},
                new object[] {0.0123456789m, new BigInteger(123456789), 10},
                new object[] {decimal.MaxValue, new BigInteger(decimal.MaxValue), 0},
                new object[] {decimal.MinValue, new BigInteger(decimal.MinValue), 0},
                new object[] {decimal.MaxValue / Convert.ToDecimal(1e28), new BigInteger(decimal.MaxValue), 28},
                new object[] {decimal.MinValue / Convert.ToDecimal(1e28), new BigInteger(decimal.MinValue), 28}
            };
        }

        public static IEnumerable<object[]> Value_Scale()
        {
            return new List<object[]>
            {
                new object[] {new BigInteger(45), 42},
                new object[] {new BigInteger(45), -42},
                new object[] {new BigInteger(long.MaxValue) * 2, 42},
                new object[] {new BigInteger(long.MinValue) * 2, 42}
            };
        }

        public static IEnumerable<object[]> DecimalArithmetics => new List<object[]>
        {
            new object[] {2.5m, 0.5m},
            new object[] {0.5m, 2.5m},
            new object[] {0m, 2.5m},
            new object[] {decimal.MaxValue / 3m, decimal.MinValue / 3m},
            new object[] {decimal.MaxValue / Convert.ToDecimal(2e28), decimal.MinusOne}
        };

        public static IEnumerable<object[]> DecimalEquals => new List<object[]>
        {
            new object[] {0m, 0.0m},
            new object[] {5m, 5.0000000000000m},
            new object[] {5.0000000000000m, 5.00m}
        };

        [TestMethod]
        [DynamicData(nameof(Decimals_DecimalPlaces), DynamicDataSourceType.Method)]
        public void FromDecimal(decimal d, BigInteger intVal, int decimalPlaces)
        {
            var bd = new BigDecimal(d);
            Assert.IsTrue(bd.Equals(new BigDecimal(d)));
            Assert.AreEqual(d, bd.ToDecimal());
            Assert.AreEqual(intVal, bd.IntVal);
            Assert.AreEqual(decimalPlaces, bd.Scale);
            if (d == 0 && BigDecimal.CheckNegativeZero(d))
            {
                Assert.IsTrue(bd.IsNegativeZero);
            }
        }

        [TestMethod]
        public void NegativeZero()
        {
            Assert.IsTrue(BigDecimal.NegativeZero().IsNegativeZero);
            Assert.IsTrue(BigDecimal.NegativeZero().Equals(new BigDecimal(-1.0000000m * 0)));
            Assert.AreEqual(0m, BigDecimal.NegativeZero().ToDecimal());
            Assert.AreEqual(0, BigDecimal.NegativeZero().IntVal);
        }

        [TestMethod]
        public void DecimalZero()
        {
            Assert.IsFalse(BigDecimal.Zero().IsNegativeZero);
            Assert.AreEqual(0, BigDecimal.Zero().IntVal);
            Assert.AreEqual(0m, BigDecimal.Zero().ToDecimal());
        }

        [TestMethod]
        [DataRow(BigDecimal.MaxPrecision + 1)]
        [ExpectedException(typeof(ArgumentException))]
        public void MaximumScale(int scale)
        {
            var _ = new BigDecimal(100, scale);
        }

        [TestMethod]
        [DynamicData(nameof(DecimalArithmetics))]
        public void Division_Decimal(decimal d1, decimal d2)
        {
            var bd1 = new BigDecimal(d1);
            var bd2 = new BigDecimal(d2);
            Assert.AreEqual(d1 / d2, (bd1 / bd2).ToDecimal());
        }

        [TestMethod]
        [DynamicData(nameof(DecimalArithmetics))]
        public void Add_Decimal(decimal d1, decimal d2)
        {
            var bd1 = new BigDecimal(d1);
            var bd2 = new BigDecimal(d2);
            Assert.AreEqual(d1 + d2, (bd1 + bd2).ToDecimal());
        }

        [TestMethod]
        [DynamicData(nameof(DecimalArithmetics))]
        public void Substract_Decimal(decimal d1, decimal d2)
        {
            var bd1 = new BigDecimal(d1);
            var bd2 = new BigDecimal(d2);
            Assert.AreEqual(d1 - d2, (bd1 - bd2).ToDecimal());
        }

        [TestMethod]
        [DynamicData(nameof(DecimalArithmetics))]
        public void Multiply_Decimal(decimal d1, decimal d2)
        {
            var bd1 = new BigDecimal(d1);
            var bd2 = new BigDecimal(d2);
            Assert.AreEqual(d1 * 1.25m, (bd1 * new BigDecimal(1.25m)).ToDecimal());
            Assert.AreEqual(d2 * 1.25m, (bd2 * new BigDecimal(1.25m)).ToDecimal());
        }

        [TestMethod]
        [DynamicData(nameof(DecimalEquals))]
        public void BigDecimal_Equals(decimal d1, decimal d2)
        {
            var bd1 = new BigDecimal(d1);
            var bd2 = new BigDecimal(d2);
            Assert.IsTrue(bd1.Equals(bd2));
        }

        [TestMethod]
        [DataRow("42.", 42, 0)]
        [DataRow("42d0", 42, 0)]
        [DataRow("42d-0", 42, 0)]
        [DataRow("4.2d1", 42, 0)]
        [DataRow("0.42d2", 42, 0)]
        [DataRow("0.420d2", 420, 1)]
        [DataRow("0d5", 0, -5)]
        [DataRow("-123.456d+42", -123456, -42 + 3)]
        [DataRow("-123.456d-42", -123456, 42 + 3)]
        [DataRow("77777.7d+00700", 777777, -700 + 1)]
        [DataRow("77777.7d-00700", 777777, 700 + 1)]
        public void Parse_Valid(string text, int expectedMag, int expectedScale)
        {
            var parsed = BigDecimal.Parse(text);
            Assert.IsTrue(BigDecimal.TryParse(text, out var tryParsed));
            Assert.AreEqual(parsed, tryParsed);
            Assert.AreEqual(expectedMag, parsed.IntVal);
            Assert.AreEqual(expectedScale, parsed.Scale);
        }

        [TestMethod]
        [DataRow("0d0-3")]
        [DataRow("3.4a")]
        [DataRow("3.4+43.4+43.4+43.4+4")]
        [DataRow("3.4d3-3")]
        [DataRow("0d-3-4")]
        [DataRow("0.3.4")]
        [DataRow("0d.3")]
        [DataRow("3.4.4-3")]
        [DataRow("3.4d4.3")]
        [DataRow("123._456")]
        public void Parse_Invalid(string text)
        {
            Assert.ThrowsException<FormatException>(() => BigDecimal.Parse(text));
            Assert.IsFalse(BigDecimal.TryParse(text, out BigDecimal _));
        }

        [TestMethod]
        [DataRow("0", false)]
        [DataRow("0.", false)]
        [DataRow("0d0", false)]
        [DataRow("0d-0", false)]
        [DataRow("0.0d1", false)]
        [DataRow("-0", true)]
        [DataRow("-0.", true)]
        [DataRow("-0d0", true)]
        [DataRow("-0d-0", true)]
        [DataRow("-0.0d1", true)]
        public void Parse_Zeros(string text, bool isNegativeZero)
        {
            var parsed = BigDecimal.Parse(text);
            Assert.IsTrue(BigDecimal.Zero().Equals(parsed));
            Assert.AreEqual(isNegativeZero, parsed.IsNegativeZero);
        }

        [TestMethod]
        [DataRow("123d-3", "1.23d-1")]
        [DataRow("-123d-3", "-1.23d-1")]
        [DataRow("123d-10", "1.23d-8")]
        [DataRow("-123d-10", "-1.23d-8")]
        [DataRow("123456d10", "123456d10")]
        [DataRow("123.456d10", "123456d7")]
        [DataRow("-0", "-0d0")]
        [DataRow("0.12345d2", "12.345")]
        [DataRow("0.12345d5", "12345.0")]
        [DataRow("0.12345d4", "1234.5")]
        [DataRow("12345d-5", "1.2345d-1")]
        public void ToString_Simple(string text, string expected)
        {
            var parsed = BigDecimal.Parse(text);
            Assert.AreEqual(expected, parsed.ToString());
        }

        [TestMethod]
        [DynamicData(nameof(Value_Scale), DynamicDataSourceType.Method)]
        public void ValueLong(BigInteger value, int scale)
        {
            var bd = new BigDecimal(value, scale);
            Assert.IsTrue(bd.Equals(new BigDecimal(value, scale)));
        }
    }
}
