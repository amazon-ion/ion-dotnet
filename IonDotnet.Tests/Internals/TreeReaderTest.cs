using IonDotnet.Builders;
using IonDotnet.Tests.Common;
using IonDotnet.Tree;
using IonDotnet.Tree.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IonDotnet.Internals.Tree;
using System;
using System.Collections.Generic;
using System.Text;

namespace IonDotnet.Tests.Internals
{
    [TestClass]
    public class TreeReaderTest
    {

        private IValueFactory _ionValueFactory = new ValueFactory();

        [TestMethod]
        public void SingleIntNumberTest()
        {
            var value = (IIonValue)_ionValueFactory.NewInt(123);
            var reader = new UserTreeReader(value);

            ReaderTestCommon.SingleNumber(reader, 123);
        }

        [TestMethod]
        public void SingleDecimalNumberTest()
        {
            var decimalValue = new BigDecimal(decimal.MaxValue);
            var value = (IIonValue)_ionValueFactory.NewDecimal(decimalValue);
            var reader = new UserTreeReader(value);

            Assert.AreEqual(IonType.Decimal, reader.MoveNext());
            Assert.AreEqual(decimalValue, reader.DecimalValue());
        }


        [TestMethod]
        public void SingleDoubleNumberTest()
        {
            var value = (IIonValue)_ionValueFactory.NewFloat(123.456);
            var reader = new UserTreeReader(value);

            Assert.AreEqual(IonType.Float, reader.MoveNext());
            Assert.AreEqual(123.456, reader.DoubleValue());
        }

        [TestMethod]
        public void TimestampTest()
        {
            var timestamp = new Timestamp(DateTime.Now);
            var value = (IIonValue)_ionValueFactory.NewTimestamp(timestamp);
            var reader = new UserTreeReader(value);

            Assert.AreEqual(IonType.Timestamp, reader.MoveNext());
            Assert.AreEqual(timestamp, reader.TimestampValue());
        }

        [TestMethod]
        public void BoolValueTest()
        {
            var value = (IIonValue)_ionValueFactory.NewBool(true);
            var reader = new UserTreeReader(value);

            ReaderTestCommon.SingleBool(reader, true);
        }

        [TestMethod]
        public void StringValueTest()
        {
            var value = (IIonValue)_ionValueFactory.NewString("test");
            var reader = new UserTreeReader(value);

            Assert.AreEqual(IonType.String, reader.MoveNext());
            Assert.AreEqual("test", reader.StringValue());
        }

        [TestMethod]
        public void NullValueTest()
        {
            var value = (IIonValue)_ionValueFactory.NewNull();
            var reader = new UserTreeReader(value);

            Assert.AreEqual(IonType.Null, reader.MoveNext());
            Assert.IsTrue(reader.CurrentIsNull);
        }

        [TestMethod]
        public void ListOfIntsTest()
        {
            var value = (IIonValue)_ionValueFactory.NewEmptyList();
            value.Add((IIonValue)_ionValueFactory.NewInt(123));
            value.Add((IIonValue)_ionValueFactory.NewInt(456));
            value.Add((IIonValue)_ionValueFactory.NewInt(789));
            var reader = new UserTreeReader(value);

            ReaderTestCommon.FlatIntList(reader);
        }

    }
}
