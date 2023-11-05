/*
 * Copyright 2020 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

using Amazon.IonDotnet.Builders;
using Amazon.IonDotnet.Tree;
using Amazon.IonDotnet.Tree.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Amazon.IonDotnet.Tests.Internals
{
    [TestClass]
    public class TextWriterJsonTest
    {
        private static readonly ValueFactory factory = new ValueFactory();
        private StringWriter sw;
        private IIonWriter jsonWriter;
        private IIonValue value;

        [TestInitialize]
        public void Initialize()
        {
            this.sw = new StringWriter();
            this.jsonWriter = IonTextWriterBuilder.Build(this.sw, IonTextOptions.Json);
            this.value = factory.NewEmptyStruct();
        }

        [TestCleanup]
        public void Cleanup()
        {
            this.jsonWriter.Dispose();
            this.sw.Dispose();
        }

        [TestMethod]
        [DataRow("0.2")]
        [DataRow("2.d-1")]
        [DataRow("2d-1")]
        public void TestInvalidJsonDecimalFromIon(string decimalString)
        {
            var bigDecimal = BigDecimal.Parse(decimalString);
            
            value.SetField("value", factory.NewDecimal(bigDecimal));
            var reader = IonReaderBuilder.Build(value);
            jsonWriter.WriteValues(reader);
            
            Assert.AreEqual("{\"value\":2e-1}", this.sw.ToString());
        }

        [TestMethod]
        public void TestGenericNull()
        {
            value.SetField("value", factory.NewNull());
            var reader = IonReaderBuilder.Build(value);
            jsonWriter.WriteValues(reader);
            Assert.AreEqual("{\"value\":null}", this.sw.ToString());
        }

        [TestMethod]
        public void TestTypedNull()
        {
            value.SetField("value", factory.NewNullBool());
            var reader = IonReaderBuilder.Build(value);
            jsonWriter.WriteValues(reader);
            Assert.AreEqual("{\"value\":null}", this.sw.ToString());
        }

        [TestMethod]
        public void TestInt()
        {
            value.SetField("value", factory.NewInt(-123));
            var reader = IonReaderBuilder.Build(value);
            jsonWriter.WriteValues(reader);
            Assert.AreEqual("{\"value\":-123}", this.sw.ToString());
        }

        [TestMethod]
        public void TestFloat()
        {
            value.SetField("value", factory.NewFloat(-123.456789));
            var reader = IonReaderBuilder.Build(value);
            jsonWriter.WriteValues(reader);
            Assert.AreEqual("{\"value\":-123.456789e0}", this.sw.ToString());
        }

        [TestMethod]
        [DataRow(double.NaN)]
        [DataRow(double.PositiveInfinity)]
        [DataRow(double.NegativeInfinity)]
        public void TestNullFloat(double doubleValue)
        {
            value.SetField("value", factory.NewFloat(doubleValue));
            var reader = IonReaderBuilder.Build(value);
            jsonWriter.WriteValues(reader);
            Assert.AreEqual("{\"value\":null}", this.sw.ToString());
        }

        [TestMethod]
        [DataRow("1.23456d2")]
        [DataRow("1.23456d+2")]
        [DataRow("12345.6d-2")]
        public void TestDecimal(string decimalString)
        {
            var bigDecimal = BigDecimal.Parse(decimalString);
            value.SetField("value", factory.NewDecimal(bigDecimal));
            var reader = IonReaderBuilder.Build(value);
            jsonWriter.WriteValues(reader);
            Assert.AreEqual("{\"value\":123.456}", this.sw.ToString());
        }

        [TestMethod]
        public void TestBigDecimal()
        {
            var bigDecimal = BigDecimal.Parse("123.456d101");
            value.SetField("value", factory.NewDecimal(bigDecimal));
            var reader = IonReaderBuilder.Build(value);
            jsonWriter.WriteValues(reader);
            Assert.AreEqual("{\"value\":123456e98}", this.sw.ToString());
        }

        [TestMethod]
        public void TestWriteDecimal()
        {
            jsonWriter.WriteDecimal(123.456m);
            Assert.AreEqual("123.456", this.sw.ToString());
        }

        [TestMethod]
        public void TestTimestamp()
        {
            DateTime time = new DateTime(2010, 6, 15, 3, 30, 45);
            Timestamp ts = new Timestamp(time);
            value.SetField("value", factory.NewTimestamp(ts));
            var reader = IonReaderBuilder.Build(value);
            jsonWriter.WriteValues(reader);
            Assert.AreEqual("{\"value\":\"2010-06-15T03:30:45.0000000-00:00\"}", this.sw.ToString());
        }

        [DataTestMethod]
        [DataRow(Timestamp.Precision.Year, "{\"value\":\"2010T\"}")]
        [DataRow(Timestamp.Precision.Month, "{\"value\":\"2010-06T\"}")]
        [DataRow(Timestamp.Precision.Day, "{\"value\":\"2010-06-15\"}")]
        [DataRow(Timestamp.Precision.Minute, "{\"value\":\"2010-06-15T03:30-00:00\"}")]
        public void TestTimestampPrecision(Timestamp.Precision p, string expected)
        {
            Timestamp ts = new Timestamp(2010, 6, 15, 3, 30, 45, p);
            value.SetField("value", factory.NewTimestamp(ts));
            var reader = IonReaderBuilder.Build(value);
            jsonWriter.WriteValues(reader);
            Assert.AreEqual(expected, this.sw.ToString());
        }

        [TestMethod]
        public void TestMinutePrecisionTimestamp()
        {
            var p = Timestamp.Precision.Minute;
            Timestamp ts = new Timestamp(2010, 6, 15, 3, 30, 45, 5 * 60, 0, p);
            value.SetField("value", factory.NewTimestamp(ts));
            var reader = IonReaderBuilder.Build(value);
            jsonWriter.WriteValues(reader);
            Assert.AreEqual("{\"value\":\"2010-06-15T03:30+05:00\"}", this.sw.ToString());
        }

        [TestMethod]
        public void TestMinutePrecisionTimestampUtc()
        {
            var p = Timestamp.Precision.Minute;
            Timestamp ts =
                new Timestamp(2010, 6, 15, 3, 30, 45, 0, 0, p, DateTimeKind.Utc);
            value.SetField("value", factory.NewTimestamp(ts));
            var reader = IonReaderBuilder.Build(value);
            jsonWriter.WriteValues(reader);
            Assert.AreEqual("{\"value\":\"2010-06-15T03:30Z\"}", this.sw.ToString());
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void TestClob()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("Ion");
            value.SetField("value", factory.NewClob(bytes));
            var reader = IonReaderBuilder.Build(value);
            jsonWriter.WriteValues(reader);
            Assert.Fail();
        }

        [TestMethod]
        public void TestBlob()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("Ion");
            value.SetField("value", factory.NewBlob(bytes));
            var reader = IonReaderBuilder.Build(value);
            jsonWriter.WriteValues(reader);
            Assert.AreEqual("{\"value\":\"SW9u\"}", this.sw.ToString());
        }

        [TestMethod]
        public void TestStruct()
        {
            IIonValue nestedStruct = factory.NewEmptyStruct();
            nestedStruct.SetField("nestedString", factory.NewString("Ion"));
            nestedStruct.SetField("nestedInt", factory.NewInt(123));
            value.SetField("value", nestedStruct);
            var reader = IonReaderBuilder.Build(value);
            jsonWriter.WriteValues(reader);
            Assert.AreEqual("{\"value\":{\"nestedString\":\"Ion\",\"nestedInt\":123}}", this.sw.ToString());
        }

        [TestMethod]
        public void TestList()
        {
            IIonValue list = factory.NewEmptyList();
            list.Add(factory.NewString("Ion"));
            list.Add(factory.NewInt(123));
            value.SetField("value", list);
            var reader = IonReaderBuilder.Build(value);
            jsonWriter.WriteValues(reader);
            Assert.AreEqual("{\"value\":[\"Ion\",123]}", this.sw.ToString());
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void TestSexp()
        {
            value.SetField("value", factory.NewEmptySexp());
            var reader = IonReaderBuilder.Build(value);
            jsonWriter.WriteValues(reader);
            Assert.Fail();
        }

        [TestMethod]
        public void TestAnnotation()
        {
            var ionInt = factory.NewInt(123);
            ionInt.AddTypeAnnotation("annotation");
            value.SetField("value", ionInt);
            var reader = IonReaderBuilder.Build(value);
            jsonWriter.WriteValues(reader);
            // They should be suppressed
            Assert.AreEqual("{\"value\":123}", this.sw.ToString());
        }

        [TestMethod]
        public void TestSymbol()
        {
            var symbol = factory.NewSymbol("symbol");
            value.SetField("value", symbol);
            var reader = IonReaderBuilder.Build(value);
            jsonWriter.WriteValues(reader);
            Assert.AreEqual("{\"value\":\"symbol\"}", this.sw.ToString());
        }

        [TestMethod]
        [DataRow("0.65", "en-US")]
        [DataRow("6.5d-1", "en-US")]
        [DataRow("0.65", "sv-SE")]
        [DataRow("6.5d-1", "sv-SE")]
        public void TestInvalidJsonDecimalFromIonWithDifferentCultures(string decimalString, string culture)
        {
            CultureInfo originalCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
            var bigDecimal = BigDecimal.Parse(decimalString);

            value.SetField("value", factory.NewDecimal(bigDecimal));
            var reader = IonReaderBuilder.Build(value);
            jsonWriter.WriteValues(reader);

            System.Threading.Thread.CurrentThread.CurrentCulture = originalCulture;
            Assert.AreEqual("{\"value\":6.5e-1}", this.sw.ToString());
        }
    }
}
