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

using System;
using System.Linq;
using System.Numerics;
using Amazon.IonDotnet.Builders;
using Amazon.IonDotnet.Internals.Tree;
using Amazon.IonDotnet.Tests.Common;
using Amazon.IonDotnet.Tree;
using Amazon.IonDotnet.Tree.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.IonDotnet.Tests.Internals
{
    [TestClass]
    public class TreeReaderTest
    {

        private readonly IValueFactory _ionValueFactory = new ValueFactory();

        [TestMethod]
        public void SingleIntNumberTest()
        {
            var value = _ionValueFactory.NewInt(123);
            var reader = new UserTreeReader(value);

            ReaderTestCommon.SingleNumber(reader, 123);
        }

        [TestMethod]
        public void SingleDecimalNumberTest()
        {
            var decimalValue = new BigDecimal(decimal.MaxValue);
            var value = _ionValueFactory.NewDecimal(decimalValue);
            var reader = new UserTreeReader(value);

            Assert.AreEqual(IonType.Decimal, reader.MoveNext());
            Assert.AreEqual(decimalValue, reader.DecimalValue());
        }


        [TestMethod]
        public void SingleDoubleNumberTest()
        {
            var value = _ionValueFactory.NewFloat(123.456);
            var reader = new UserTreeReader(value);

            Assert.AreEqual(IonType.Float, reader.MoveNext());
            Assert.AreEqual(123.456, reader.DoubleValue());
        }

        [TestMethod]
        public void TimestampTest()
        {
            var timestamp = new Timestamp(DateTime.Now);
            var value = _ionValueFactory.NewTimestamp(timestamp);
            var reader = new UserTreeReader(value);

            Assert.AreEqual(IonType.Timestamp, reader.MoveNext());
            Assert.AreEqual(timestamp, reader.TimestampValue());
        }

        [TestMethod]
        public void BoolValueTest()
        {
            var value = _ionValueFactory.NewBool(true);
            var reader = new UserTreeReader(value);

            ReaderTestCommon.SingleBool(reader, true);
        }

        [TestMethod]
        public void StringValueTest()
        {
            var value = _ionValueFactory.NewString("test");
            var reader = new UserTreeReader(value);

            Assert.AreEqual(IonType.String, reader.MoveNext());
            Assert.AreEqual("test", reader.StringValue());
        }

        [TestMethod]
        public void NullValueTest()
        {
            var value = _ionValueFactory.NewNull();
            var reader = new UserTreeReader(value);

            Assert.AreEqual(IonType.Null, reader.MoveNext());
            Assert.IsTrue(reader.CurrentIsNull);
        }

        [TestMethod]
        public void CurrentTypeTest()
        {
            var value = _ionValueFactory.NewBool(true);
            var reader = new UserTreeReader(value);
            Assert.AreEqual(IonType.None, reader.CurrentType);

            reader.MoveNext();
            Assert.AreEqual(IonType.Bool, reader.CurrentType);
        }

        [TestMethod]
        public void ListOfIntsTest()
        {
            //Must be: [123,456,789]
            var value = _ionValueFactory.NewEmptyList();
            value.Add(_ionValueFactory.NewInt(123));
            value.Add(_ionValueFactory.NewInt(456));
            value.Add(_ionValueFactory.NewInt(789));
            var reader = new UserTreeReader(value);

            ReaderTestCommon.FlatIntList(reader);
        }

        [TestMethod]
        public void SimpleDatagramTest()
        {
            //simple datagram: {yolo:true}
            var value = new IonStruct{{ "yolo", _ionValueFactory.NewBool(true) }};
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
                { "boolean", _ionValueFactory.NewBool(true) },
                { "str", _ionValueFactory.NewString("yes") },
                { "integer", _ionValueFactory.NewInt(123456) },
                { "longInt", _ionValueFactory.NewInt((long)int.MaxValue * 2) },
                { "bigInt", _ionValueFactory.NewInt(BigInteger.Multiply(new BigInteger(long.MaxValue), 10)) },
                { "double", _ionValueFactory.NewFloat(2213.1267567) }
            };
            var reader = new UserTreeReader(value);

            ReaderTestCommon.FlatScalar(reader);
        }

        [TestMethod]
        public void SingleSymbolTest()
        {
            //{single_symbol:'something'}
            var value = new IonStruct {{ "single_symbol", _ionValueFactory.NewSymbol("something")}};
            var reader = new UserTreeReader(value);

            ReaderTestCommon.SingleSymbol(reader);
        }

        [TestMethod]
        public void NestedAndCombinedListStructTest()
        {
            //Must be:
            // {
            //   menu: {  
            //     id: "file",
            //     popup: [    
            //       "Open",
            //       "Load",
            //       "Close"
            //     ],
            //     deep1: {    
            //       deep2: {      
            //         deep3: {        
            //           deep4val: "enddeep"
            //         }
            //       }
            //     },
            //     positions: [    
            //       1234,
            //       5678,
            //       90
            //     ]
            //   }
            // }

            var popupList = _ionValueFactory.NewEmptyList();
            popupList.Add(_ionValueFactory.NewString("Open"));
            popupList.Add(_ionValueFactory.NewString("Load"));
            popupList.Add(_ionValueFactory.NewString("Close"));

            var positionList = _ionValueFactory.NewEmptyList();
            positionList.Add(_ionValueFactory.NewInt(1234));
            positionList.Add(_ionValueFactory.NewInt(5678));
            positionList.Add(_ionValueFactory.NewInt(90));

            var deep3 = new IonStruct {{ "deep4val", _ionValueFactory.NewString("enddeep") }};

            var deep2 = new IonStruct {{ "deep3", deep3 }};

            var deep1 = new IonStruct {{ "deep2", deep2 }};

            var menu = new IonStruct
            {
                { "id", _ionValueFactory.NewString("file") },
                { "popup", popupList },
                { "deep1", deep1 },
                { "positions", positionList }
            };

            var value = new IonStruct {{ "menu", menu }};
            var reader = new UserTreeReader(value);

            ReaderTestCommon.Combined1(reader);
        }

        [TestMethod]
        public void ValueWithTypeAnnotationsTest()
        {
            //Must be: {withannot: years::months::days::hours::minutes::seconds::18}
            var intValue = _ionValueFactory.NewInt(18);
            intValue.AddTypeAnnotation("years");
            intValue.AddTypeAnnotation("months");
            intValue.AddTypeAnnotation("days");
            intValue.AddTypeAnnotation("hours");
            intValue.AddTypeAnnotation("minutes");
            intValue.AddTypeAnnotation("seconds");
            var value = new IonStruct { { "withannot", intValue } };
            var reader = new UserTreeReader(value);

            ReaderTestCommon.ReadTypeAnnotations_SingleField(reader);
        }

        [TestMethod]
        public void ValueWithTypeAnnotationsTest_ZeroSymbol()
        {
            //Must be: $0::18
            var intValue = _ionValueFactory.NewInt(18);
            intValue.AddTypeAnnotation(new SymbolToken(null, 0));
            var reader = new UserTreeReader(intValue);

            ReaderTestCommon.ReadTypeAnnotations_ZeroSymbol(reader);
        }

        [TestMethod]
        public void ValueWithTypeAnnotationsTest_AssertUnknownSymbolException()
        {
            string input = "$ion_symbol_table::{ imports:[{ name: \"abc\", version: 1, max_id: 1}],symbols: [\"foo\"]}$10::$11::\"value\"";
            IIonValue data = IonLoader.Default.Load(input);

            UserTreeReader reader = new UserTreeReader(data);

            ReaderTestCommon.ReadTypeAnnotations_AssertUnknownSymbolException(reader);
        }

        [TestMethod]
        public void ValueWithTypeAnnotationSymbolsTest()
        {
            //Must be: {withannot: years::months::days::hours::minutes::seconds::18}
            var intValue = _ionValueFactory.NewInt(18);
            intValue.AddTypeAnnotation("years");
            intValue.AddTypeAnnotation("months");
            intValue.AddTypeAnnotation("days");
            intValue.AddTypeAnnotation("hours");
            intValue.AddTypeAnnotation("minutes");
            intValue.AddTypeAnnotation("seconds");
            var value = new IonStruct {{ "withannot", intValue }};
            var reader = new UserTreeReader(value);

            ReaderTestCommon.ReadTypeAnnotationSymbols_SingleField(reader);
        }

        [TestMethod]
        public void ValueWithTypeAnnotationSymbolsTest_ZeroSymbol()
        {
            //Must be: $0::18
            var intValue = _ionValueFactory.NewInt(18);
            intValue.AddTypeAnnotation(new SymbolToken(null, 0));
            var reader = new UserTreeReader(intValue);

            ReaderTestCommon.ReadTypeAnnotationSymbols_ZeroSymbol(reader);
        }

        [TestMethod]
        public void ValueWithTypeAnnotationSymbolsTest_AssertNoUnknownSymbolException()
        {
            string input = "$ion_symbol_table::{ imports:[{ name: \"abc\", version: 1, max_id: 1}],symbols: [\"foo\"]}$10::$11::\"value\"";
            IIonValue data = IonLoader.Default.Load(input);

            UserTreeReader reader = new UserTreeReader(data);

            ReaderTestCommon.ReadTypeAnnotationSymbols_AssertNoUnknownSymbolException(reader);
        }

        [TestMethod]
        public void HasAnnotationTrueTest()
        {
            //Must be: {withannot: years::months::days::hours::minutes::seconds::18}
            var intValue = _ionValueFactory.NewInt(18);
            intValue.AddTypeAnnotation("years");
            intValue.AddTypeAnnotation("months");
            intValue.AddTypeAnnotation("days");
            intValue.AddTypeAnnotation("hours");
            intValue.AddTypeAnnotation("minutes");
            intValue.AddTypeAnnotation("seconds");
            var value = new IonStruct { { "withannot", intValue } };
            var reader = new UserTreeReader(value);

            ReaderTestCommon.HasAnnotationTrue_SingleField(reader);
        }

        [TestMethod]
        public void HasAnnotationFalseTest()
        {
            //Must be: {withannot: years::months::days::hours::minutes::seconds::18}
            var intValue = _ionValueFactory.NewInt(18);
            intValue.AddTypeAnnotation("years");
            intValue.AddTypeAnnotation("months");
            intValue.AddTypeAnnotation("days");
            intValue.AddTypeAnnotation("hours");
            intValue.AddTypeAnnotation("minutes");
            intValue.AddTypeAnnotation("seconds");
            var value = new IonStruct { { "withannot", intValue } };
            var reader = new UserTreeReader(value);

            ReaderTestCommon.HasAnnotationFalse_SingleField(reader);
        }

        [TestMethod]
        public void HasAnnotation_AssertUnknownSymbolException()
        {
            string input = "$ion_symbol_table::{ imports:[{ name: \"abc\", version: 1, max_id: 1}],symbols: [\"foo\"]}$10::$11::\"value\"";
            IIonValue data = IonLoader.Default.Load(input);

            UserTreeReader reader = new UserTreeReader(data);

            ReaderTestCommon.HasAnnotation_AssertUnknownSymbolException(reader);
        }

        [TestMethod]
        public void HasAnnotationTrueTest_ZeroSymbol()
        {
            //Must be: $0::18
            var intValue = _ionValueFactory.NewInt(18);
            intValue.AddTypeAnnotation(new SymbolToken(null, 0));
            var reader = new UserTreeReader(intValue);

            ReaderTestCommon.HasAnnotation_ZeroSymbol(reader);
        }

        [TestMethod]
        public void BlobTest()
        {
            //Must be in a struct:
            // { blobbbb: {{data}} }
            var arrayOfbytes = Enumerable.Repeat<byte>(1, 100).ToArray();         
            ReadOnlySpan<byte> bytes = new ReadOnlySpan<byte>(arrayOfbytes);
            var blob = _ionValueFactory.NewBlob(bytes);
            var value = new IonStruct {{ "blobbbb", blob }};
            var reader = new UserTreeReader(value);

            ReaderTestCommon.Struct_OneBlob(reader);
        }

        [TestMethod]
        public void BlobPartialReadTest()
        {
            var blob = new byte[30];
            for (var i = 0; i < 30; i++)
            {
                blob[i] = (byte)i;
            }
            var value = _ionValueFactory.NewBlob(blob);
            var reader = new UserTreeReader(value);

            ReaderTestCommon.Blob_PartialRead(30, 7, reader);
        }

        [TestMethod]
        public void NullParentHasNext()
        {
            var value = _ionValueFactory.NewInt(123);
            var reader = new UserTreeReader(value);

            Assert.AreEqual(IonType.Int, reader.MoveNext());
            Assert.IsFalse(reader.HasNext());
        }
    }
}
