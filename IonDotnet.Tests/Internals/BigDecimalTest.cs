using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            Assert.IsTrue(BigDecimal.NegativeZero.IsNegativeZero);
            Assert.AreEqual(0m, BigDecimal.NegativeZero.ToDecimal());
            Assert.AreEqual(0, BigDecimal.NegativeZero.IntVal);
            Assert.AreEqual(0, BigDecimal.NegativeZero.Scale);
        }

        [TestMethod]
        [DataRow(BigDecimal.MaxPrecision + 1)]
        [DataRow(-BigDecimal.MaxPrecision - 1)]
        [ExpectedException(typeof(ArgumentException))]
        public void MaximumScale(int scale)
        {
            var _ = new BigDecimal(100, scale);
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
