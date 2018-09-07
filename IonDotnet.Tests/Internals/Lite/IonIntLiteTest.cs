using System;
using System.Numerics;
using IonDotnet.Internals.Lite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Internals.Lite
{
//    [TestClass]
//    public class IonIntLiteTest
//    {
//        private static readonly ContainerlessContext Ctx = new ContainerlessContext(null);
//        private static readonly IIonSystem IonSys = new IonSystemLite(Ctx);
//
//        [TestMethod]
//        public void NullValue()
//        {
//            var ionValue = IonSys.NewNullInt();
//            AssertNull(ionValue);
//        }
//
//        [TestMethod]
//        [DataRow(0)]
//        [DataRow(1)]
//        [DataRow(2)]
//        [DataRow(1233213)]
//        [DataRow(-1233213)]
//        [DataRow(int.MaxValue)]
//        public void Mutation_Valid(int seed)
//        {
//            var ionValue = IonSys.NewNullInt();
//            ionValue.IntValue = seed;
//            Assert.AreEqual(IntegerSize.Int, ionValue.Size);
//            Assert.AreEqual(seed, ionValue.IntValue);
//            Assert.AreEqual(seed, ionValue.LongValue);
//            Assert.AreEqual(seed, ionValue.BigIntegerValue);
//
//            var longSeed = Math.Abs((long) seed) + int.MaxValue + 1;
//            ionValue.LongValue = longSeed;
//            Assert.AreEqual(IntegerSize.Long, ionValue.Size);
//            Assert.AreNotEqual(longSeed, ionValue.IntValue);
//            Assert.AreEqual(longSeed, ionValue.LongValue);
//            Assert.AreEqual(longSeed, ionValue.BigIntegerValue);
//
//            var bigSeed = new BigInteger(Math.Abs(seed)) + long.MaxValue + 1;
//            ionValue.BigIntegerValue = bigSeed;
//            Assert.AreEqual(IntegerSize.BigInteger, ionValue.Size);
//            Assert.ThrowsException<OverflowException>(() => ionValue.IntValue);
//            Assert.ThrowsException<OverflowException>(() => ionValue.LongValue);
//            Assert.AreEqual(bigSeed, ionValue.BigIntegerValue);
//
//            ionValue.SetNull();
//            AssertNull(ionValue);
//        }
//
//        [TestMethod]
//        [DataRow(null, null)]
//        [DataRow(52, null)]
//        [DataRow(52, long.MaxValue / 2)]
//        [DataRow(null, long.MaxValue / 3)]
//        public void ReadOnly_TrySet_ThrowsException(int? intVal, long? longVal)
//        {
//            var ionInt = intVal == null ? IonSys.NewNullInt() : IonSys.NewInt(intVal.Value);
//            var ionLong = longVal == null ? IonSys.NewNullInt() : IonSys.NewInt(longVal.Value);
//
//            ionInt.MakeReadOnly();
//            ionLong.MakeReadOnly();
//
//            AssertReadOnly(ionInt);
//            AssertReadOnly(ionLong);
//        }
//
//        private static void AssertNull(IIonInt ionValue)
//        {
//            Assert.IsTrue(ionValue.IsNull);
//            Assert.ThrowsException<NullValueException>(() => ionValue.IntValue);
//            Assert.ThrowsException<NullValueException>(() => ionValue.LongValue);
//            Assert.ThrowsException<NullValueException>(() => ionValue.BigIntegerValue);
//        }
//
//        private static void AssertReadOnly(IIonInt ionInt)
//        {
//            Assert.IsTrue(ionInt.ReadOnly);
//            Assert.ThrowsException<InvalidOperationException>(() => ionInt.IntValue = int.MaxValue / 4);
//            Assert.ThrowsException<InvalidOperationException>(() => ionInt.LongValue = long.MaxValue / 4);
//            Assert.ThrowsException<InvalidOperationException>(() => ionInt.BigIntegerValue = new BigInteger(long.MaxValue));
//        }
//    }
}
