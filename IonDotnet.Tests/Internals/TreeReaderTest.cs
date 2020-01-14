﻿using IonDotnet.Builders;
using IonDotnet.Tests.Common;
using IonDotnet.Tree;
using IonDotnet.Tree.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IonDotnet.Internals.Tree;
using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

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
            //Must be: [123,456,789]
            var value = (IIonValue)_ionValueFactory.NewEmptyList();
            value.Add((IIonValue)_ionValueFactory.NewInt(123));
            value.Add((IIonValue)_ionValueFactory.NewInt(456));
            value.Add((IIonValue)_ionValueFactory.NewInt(789));
            var reader = new UserTreeReader(value);

            ReaderTestCommon.FlatIntList(reader);
        }

        [TestMethod]
        public void SimpleDatagramTest()
        {
            //simple datagram: {yolo:true}
            var value = new IonStruct{{ "yolo", (IIonValue)_ionValueFactory.NewBool(true) }};
            var reader = new UserTreeReader(value);

            ReaderTestCommon.OneBoolInStruct(reader);
        }

        [TestMethod]
        public void FlatStructScalarTest()
        {
            //Must be a flat struct of scalar values:
            //boolean:true
            //str:"yes"
            //integer:123456
            //longInt:int.Max*2
            //bigInt:long.Max*10
            //double:2213.1267567f
            var value = new IonStruct
            {
                { "boolean", (IIonValue)_ionValueFactory.NewBool(true) },
                { "str", (IIonValue)_ionValueFactory.NewString("yes") },
                { "integer", (IIonValue)_ionValueFactory.NewInt(123456) },
                { "longInt", (IIonValue)_ionValueFactory.NewInt((long)int.MaxValue * 2) },
                { "bigInt", (IIonValue)_ionValueFactory.NewInt(BigInteger.Multiply(new BigInteger(long.MaxValue), 10)) },
                { "double", (IIonValue)_ionValueFactory.NewFloat(2213.1267567) }
            };
            var reader = new UserTreeReader(value);

            ReaderTestCommon.FlatScalar(reader);
        }

        [TestMethod]
        public void SingleSymbolTest()
        {
            //{single_symbol:'something'}
            var value = new IonStruct {{ "single_symbol", (IIonValue)_ionValueFactory.NewSymbol("something")}};
            var reader = new UserTreeReader(value);

            ReaderTestCommon.SingleSymbol(reader);
        }

        [TestMethod]
        public void NestedAndCombinedListStructTest()
        {
            var popupList = (IIonValue)_ionValueFactory.NewEmptyList();
            popupList.Add((IIonValue)_ionValueFactory.NewString("open"));
            popupList.Add((IIonValue)_ionValueFactory.NewString("load"));
            popupList.Add((IIonValue)_ionValueFactory.NewString("close"));

            var positionList = (IIonValue)_ionValueFactory.NewEmptyList();
            positionList.Add((IIonValue)_ionValueFactory.NewInt(1234));
            positionList.Add((IIonValue)_ionValueFactory.NewInt(5678));
            positionList.Add((IIonValue)_ionValueFactory.NewInt(90));

            var deep3 = new IonStruct {{ "deep4val", (IIonValue)_ionValueFactory.NewString("enddeep") }};

            var deep2 = new IonStruct
            { { "deep4val", (IIonValue)_ionValueFactory.NewString("enddeep") } };

        }

    }
}