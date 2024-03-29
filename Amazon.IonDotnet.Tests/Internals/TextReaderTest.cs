﻿/*
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
using System.IO;
using System.Text;
using Amazon.IonDotnet.Internals.Text;
using Amazon.IonDotnet.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Amazon.IonDotnet.Tests.Internals
{
    [TestClass]
    public class TextReaderTest
    {
        [TestMethod]
        public void NonReadableStream_ThrowsArgumentException()
        {
            var mockStream = new Mock<Stream>();
            mockStream.Setup(s => s.CanRead).Returns(false);
            Assert.ThrowsException<ArgumentException>(() => new UserTextReader(mockStream.Object));
        }

        [TestMethod]
        public void Empty()
        {
            var bytes = new byte[0];
            var reader = new UserTextReader(new MemoryStream(bytes));
            ReaderTestCommon.Empty(reader);
        }

        [TestMethod]
        public void TrivialStruct()
        {
            //empty struct {}
            var trivial = DirStructure.OwnTestFileAsBytes("text/trivial.ion");
            var text = Encoding.UTF8.GetString(trivial);

            var reader = new UserTextReader(text);
            ReaderTestCommon.TrivialStruct(reader);

            reader = new UserTextReader(new MemoryStream(trivial));
            ReaderTestCommon.TrivialStruct(reader);
        }

        /// <summary>
        /// Test for single-value bool 
        /// </summary>
        [TestMethod]
        [DataRow("true", true)]
        [DataRow("false", false)]
        public void SingleBool(string text, bool value)
        {
            var bin = Encoding.UTF8.GetBytes(text);
            var reader = new UserTextReader(new MemoryStream(bin));
            ReaderTestCommon.SingleBool(reader, value);

            reader = new UserTextReader(text);
            ReaderTestCommon.SingleBool(reader, value);
        }


        [TestMethod]
        [DataRow("1234567890", 1234567890)]
        [DataRow("4611686018427387903", long.MaxValue / 2)]
        public void SingleNumber(string text, long value)
        {
            var bin = Encoding.UTF8.GetBytes(text);
            var reader = new UserTextReader(new MemoryStream(bin));
            ReaderTestCommon.SingleNumber(reader, value);

            reader = new UserTextReader(text);
            ReaderTestCommon.SingleNumber(reader, value);
        }

        [TestMethod]
        public void CurrentType()
        {
            var bin = Encoding.UTF8.GetBytes("true");
            var reader = new UserTextReader(new MemoryStream(bin));
            Assert.AreEqual(IonType.None, reader.CurrentType);

            reader.MoveNext();
            Assert.AreEqual(IonType.Bool, reader.CurrentType);
        }

        [TestMethod]
        public void OneBoolInStruct()
        {
            //simple datagram: {yolo:true}
            var oneBool = DirStructure.OwnTestFileAsBytes("text/onebool.ion");
            var reader = new UserTextReader(new MemoryStream(oneBool));
            ReaderTestCommon.OneBoolInStruct(reader);

            reader = new UserTextReader(Encoding.UTF8.GetString(oneBool));
            ReaderTestCommon.OneBoolInStruct(reader);
        }

        [TestMethod]
        public void FlatScalar()
        {
            //a flat struct of scalar values:
            //boolean:true
            //str:"yes"
            //integer:123456
            //longInt:int.Max*2
            //bigInt:long.Max*10
            //double:2213.1267567f
            var flatScalar = DirStructure.OwnTestFileAsBytes("text/flat_scalar.ion");

            var reader = new UserTextReader(new MemoryStream(flatScalar));
            ReaderTestCommon.FlatScalar(reader);

            reader = new UserTextReader(Encoding.UTF8.GetString(flatScalar));
            ReaderTestCommon.FlatScalar(reader);
        }

        [TestMethod]
        public void FlatIntList()
        {
            //a flat list of ints [123,456,789]
            var flatListInt = DirStructure.OwnTestFileAsBytes("text/flatlist_int.ion");

            var reader = new UserTextReader(new MemoryStream(flatListInt));
            ReaderTestCommon.FlatIntList(reader);
        }

        [TestMethod]
        public void ReadTypeAnnotations_SingleField()
        {
            // a singlefield structure with annotations
            // {withannot:years::months::days::hours::minutes::seconds::18}
            byte[] data = DirStructure.OwnTestFileAsBytes("text/annot_singlefield.ion");
            UserTextReader reader = new UserTextReader(new MemoryStream(data));

            ReaderTestCommon.ReadTypeAnnotations_SingleField(reader);
        }

        [TestMethod]
        public void ReadTypeAnnotations_ZeroSymbol()
        {
            // an int with zero symbol annotation
            // $0::18
            var reader = new UserTextReader("$0::18");

            ReaderTestCommon.ReadTypeAnnotations_ZeroSymbol(reader);
        }

        [TestMethod]
        public void ReadTypeAnnotations_AssertUnknownSymbolException()
        {
            string input = "$ion_symbol_table::{ imports:[{ name: \"abc\", version: 1, max_id: 1}],symbols: [\"foo\"]}$10::$11::\"value\"";
            UserTextReader reader = new UserTextReader(input);

            ReaderTestCommon.ReadTypeAnnotations_AssertUnknownSymbolException(reader);
        }

        [TestMethod]
        public void ReadTypeAnnotationSymbols_SingleField()
        {
            // a singlefield structure with annotations
            // {withannot:years::months::days::hours::minutes::seconds::18}
            byte[] data = DirStructure.OwnTestFileAsBytes("text/annot_singlefield.ion");
            UserTextReader reader = new UserTextReader(new MemoryStream(data));

            ReaderTestCommon.ReadTypeAnnotationSymbols_SingleField(reader);
        }

        [TestMethod]
        public void ReadTypeAnnotationSymbols_ZeroSymbol()
        {
            // an int with zero symbol annotation
            // $0::18
            var reader = new UserTextReader("$0::18");

            ReaderTestCommon.ReadTypeAnnotationSymbols_ZeroSymbol(reader);
        }

        [TestMethod]
        public void ReadTypeAnnotationSymbols_AssertNoUnknownSymbolException()
        {
            string input = "$ion_symbol_table::{ imports:[{ name: \"abc\", version: 1, max_id: 1}],symbols: [\"foo\"]}$10::$11::\"value\"";
            UserTextReader reader = new UserTextReader(input);

            ReaderTestCommon.ReadTypeAnnotationSymbols_AssertNoUnknownSymbolException(reader);
        }

        [TestMethod]
        public void HasAnnotationTrue_SingleField()
        {
            // a singlefield structure with annotations
            // {withannot:years::months::days::hours::minutes::seconds::18}
            byte[] data = DirStructure.OwnTestFileAsBytes("text/annot_singlefield.ion");
            UserTextReader reader = new UserTextReader(new MemoryStream(data));

            ReaderTestCommon.HasAnnotationTrue_SingleField(reader);
        }

        [TestMethod]
        public void HasAnnotationFalse_SingleField()
        {
            // a singlefield structure with annotations
            // {withannot:years::months::days::hours::minutes::seconds::18}
            byte[] data = DirStructure.OwnTestFileAsBytes("text/annot_singlefield.ion");
            UserTextReader reader = new UserTextReader(new MemoryStream(data));

            ReaderTestCommon.HasAnnotationFalse_SingleField(reader);
        }

        [TestMethod]
        public void HasAnnotation_ZeroSymbol()
        {
            // an int with zero symbol annotation
            // $0::18
            var reader = new UserTextReader("$0::18");

            ReaderTestCommon.HasAnnotation_ZeroSymbol(reader);
        }

        [TestMethod]
        public void HasAnnotation_AssertUnknownSymbolException()
        {
            string input = "$ion_symbol_table::{ imports:[{ name: \"abc\", version: 1, max_id: 1}],symbols: [\"foo\"]}$10::$11::\"value\"";
            UserTextReader reader = new UserTextReader(input);

            ReaderTestCommon.HasAnnotation_AssertUnknownSymbolException(reader);
        }

        [TestMethod]
        public void SingleSymbol()
        {
            //struct with single symbol
            //{single_symbol:'something'}
            var data = DirStructure.OwnTestFileAsBytes("text/single_symbol.ion");

            var reader = new UserTextReader(new MemoryStream(data));
            ReaderTestCommon.SingleSymbol(reader);

            reader = new UserTextReader(Encoding.UTF8.GetString(data));
            ReaderTestCommon.SingleSymbol(reader);
        }

        [TestMethod]
        public void SingleIntList()
        {
            var data = DirStructure.OwnTestFileAsBytes("text/single_int_list.ion");
            var reader = new UserTextReader(new MemoryStream(data));
            ReaderTestCommon.SingleIntList(reader);

            reader = new UserTextReader(Encoding.UTF8.GetString(data));
            ReaderTestCommon.SingleIntList(reader);
        }

        /// <summary>
        /// Test for a typical json-style message
        /// </summary>
        [TestMethod]
        public void Combined1()
        {
            var data = DirStructure.OwnTestFileAsBytes("text/combined1.ion");
            var reader = new UserTextReader(new MemoryStream(data));
            ReaderTestCommon.Combined1(reader);

            reader = new UserTextReader(Encoding.UTF8.GetString(data));
            ReaderTestCommon.Combined1(reader);
        }

        [TestMethod]
        public void Struct_OneBlob()
        {
            var data = DirStructure.OwnTestFileAsBytes("text/struct_oneblob.ion");
            var reader = new UserTextReader(new MemoryStream(data));
            ReaderTestCommon.Struct_OneBlob(reader);

            reader = new UserTextReader(Encoding.UTF8.GetString(data));
            ReaderTestCommon.Struct_OneBlob(reader);
        }

        [DataRow("12_34.56_78e0", 1234.5678e0)]
        [DataRow("12_34e56", 1234e56)]
        [DataRow("1_2_3_4.5_6_7_8E90", 1234.5678e90)]
        [TestMethod]
        public void Float_Underscore(string f, double val)
        {
            var reader = new UserTextReader(f);
            Assert.AreEqual(IonType.Float, reader.MoveNext());
            Assert.AreEqual(val, reader.DoubleValue());
        }

        [DataRow("good/eolCommentCrLf.ion")]
        [DataRow("good/eolCommentCr.ion")]
        [TestMethod]
        public void EolComment(string fileName)
        {
            var fileAsStream = DirStructure.IonTestFileAsStream(fileName);
            var reader = new UserTextReader(fileAsStream);
            Assert.AreEqual(IonType.List, reader.MoveNext());
            reader.StepIn();
            Assert.AreEqual(IonType.None, reader.MoveNext());
            reader.StepOut();
            Assert.AreEqual(IonType.None, reader.MoveNext());
        }

        [DataRow("good/commentMultiLineThenEof.ion")]
        [DataRow("good/commentSingleLineThenEof.ion")]
        [TestMethod]
        public void CommentThenEof(string fileName)
        {
            var fileAsStream = DirStructure.IonTestFileAsStream(fileName);
            var reader = new UserTextReader(fileAsStream);

            Assert.AreEqual(IonType.Symbol, reader.MoveNext());
            Assert.AreEqual("abc", reader.SymbolValue().Text);
            Assert.AreEqual(IonType.None, reader.MoveNext());
        }

        [TestMethod]
        public void EmptyBlob()
        {
            const string text = "{{}}";
            var reader = new UserTextReader(text);
            Assert.AreEqual(IonType.Blob, reader.MoveNext());
        }

        [TestMethod]
        [DataRow(0, 0)]
        [DataRow(100, 24)]
        public void Blob_PartialRead(int size, int step)
        {
            var blob = new byte[size];
            for (var i = 0; i < size; i++)
            {
                blob[i] = (byte)i;
            }

            var text = "{{" + Convert.ToBase64String(blob) + "}}";
            var reader = new UserTextReader(text);
            ReaderTestCommon.Blob_PartialRead(size, step, reader);
        }

        [TestMethod()]
        public void SkipOverListInStruct()
        {
            var reader = new UserTextReader("{ somelist: [ 1 ] }");
            Assert.AreEqual(IonType.Struct, reader.MoveNext());
            reader.StepIn();
            Assert.AreEqual(IonType.List, reader.MoveNext());
            Assert.AreEqual("somelist", reader.CurrentFieldName);
            // Now skip over the list instead of stepping into it
            Assert.AreEqual(IonType.None, reader.MoveNext());
            reader.StepOut(); // Step out of struct
            Assert.AreEqual(IonType.None, reader.MoveNext());
        }

        [TestMethod()]
        public void SkipOverStructInList()
        {
            var reader = new UserTextReader("[{}]");
            Assert.AreEqual(IonType.List, reader.MoveNext());
            reader.StepIn();
            Assert.AreEqual(IonType.Struct, reader.MoveNext());
            // Skip over the struct instead of stepping in
            Assert.AreEqual(IonType.None, reader.MoveNext());
            reader.StepOut(); // Step out of list
            Assert.AreEqual(IonType.None, reader.MoveNext());
        }
    }
}
