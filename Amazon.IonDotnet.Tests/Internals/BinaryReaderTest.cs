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
using System.IO;
using System.Text;
using Amazon.IonDotnet.Builders;
using Amazon.IonDotnet.Internals.Binary;
using Amazon.IonDotnet.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Amazon.IonDotnet.Tests.Internals
{
    [TestClass]
    public class UserBinaryReaderTest
    {
        [TestMethod]
        public void NonReadableStream_ThrowsArgumentException()
        {
            var mockStream = new Mock<Stream>();
            mockStream.Setup(s => s.CanRead).Returns(false);
            Assert.ThrowsException<ArgumentException>(() => new UserBinaryReader(mockStream.Object));
        }

        [TestMethod]
        public void Empty()
        {
            var bytes = new byte[0];
            var reader = new UserBinaryReader(new MemoryStream(bytes));
            ReaderTestCommon.Empty(reader);
        }

        [TestMethod]
        public void TrivialStruct()
        {
            //empty struct {}
            var trivial = DirStructure.OwnTestFileAsBytes("binary/trivial.bindat");
            var reader = new UserBinaryReader(new MemoryStream(trivial));
            ReaderTestCommon.TrivialStruct(reader);
        }

        /// <summary>
        /// Test for single-value bool 
        /// </summary>
        [TestMethod]
        [DataRow(new byte[] { 0xE0, 0x01, 0x00, 0xEA, 0x11 }, true)]
        [DataRow(new byte[] { 0xE0, 0x01, 0x00, 0xEA, 0x10 }, false)]
        public void SingleBool(byte[] data, bool value)
        {
            var reader = new UserBinaryReader(new MemoryStream(data));
            ReaderTestCommon.SingleBool(reader, value);
        }

        [TestMethod]
        [DataRow(new byte[] { 0xE0, 0x01, 0x00, 0xEA, 0x24, 0x49, 0x96, 0x02, 0xD2 }, 1234567890)]
        [DataRow(new byte[] { 0xE0, 0x01, 0x00, 0xEA, 0x28, 0x3F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, long.MaxValue / 2)]
        public void SingleNumber(byte[] data, long value)
        {
            IIonReader reader = new UserBinaryReader(new MemoryStream(data));
            ReaderTestCommon.SingleNumber(reader, value);
        }

        [TestMethod]
        public void TestCurrentType()
        {
            var reader = new UserBinaryReader(new MemoryStream(new byte[] { 0xE0, 0x01, 0x00, 0xEA, 0x11 }));
            Assert.AreEqual(IonType.None, reader.CurrentType);

            reader.MoveNext();
            Assert.AreEqual(IonType.Bool, reader.CurrentType);
        }

        [TestMethod]
        public void OneBoolInStruct()
        {
            //simple datagram: {yolo:true}
            var oneBool = DirStructure.OwnTestFileAsBytes("binary/onebool.bindat");
            var reader = new UserBinaryReader(new MemoryStream(oneBool));
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
            var flatScalar = DirStructure.OwnTestFileAsBytes("binary/flat_scalar.bindat");

            var reader = new UserBinaryReader(new MemoryStream(flatScalar));
            ReaderTestCommon.FlatScalar(reader);
        }

        [TestMethod]
        public void FlatIntList()
        {
            //a flat list of ints [123,456,789]
            var flatListInt = DirStructure.OwnTestFileAsBytes("binary/flatlist_int.bindat");

            var reader = new UserBinaryReader(new MemoryStream(flatListInt));
            ReaderTestCommon.FlatIntList(reader);
        }

        [TestMethod]
        public void ReadTypeAnnotations_SingleField()
        {
            // a singlefield structure with annotations
            // {withannot:years::months::days::hours::minutes::seconds::18}
            byte[] data = DirStructure.OwnTestFileAsBytes("binary/annot_singlefield.bindat");
            UserBinaryReader reader = new UserBinaryReader(new MemoryStream(data));

            ReaderTestCommon.ReadTypeAnnotations_SingleField(reader);
        }

        [TestMethod]
        public void ReadTypeAnnotations_ZeroSymbol()
        {
            // an int with zero symbol annotation
            // $0::18
            var data = new byte[] { 0xE4, 0x81, 0x80, 0x21, 0x12 };
            var reader = new UserBinaryReader(new MemoryStream(data));

            ReaderTestCommon.ReadTypeAnnotations_ZeroSymbol(reader);
        }

        [TestMethod]
        public void ReadTypeAnnotations_AssertUnknownSymbolException()
        {
            byte[] data = DirStructure.OwnTestFileAsBytes("binary/unknown_symbols.bindat");
            UserBinaryReader reader = new UserBinaryReader(new MemoryStream(data));

            ReaderTestCommon.ReadTypeAnnotations_AssertUnknownSymbolException(reader);
        }

        [TestMethod]
        public void ReadTypeAnnotationSymbols_SingleField()
        {
            // a singlefield structure with annotations
            // {withannot:years::months::days::hours::minutes::seconds::18}
            byte[] data = DirStructure.OwnTestFileAsBytes("binary/annot_singlefield.bindat");
            UserBinaryReader reader = new UserBinaryReader(new MemoryStream(data));

            ReaderTestCommon.ReadTypeAnnotationSymbols_SingleField(reader);
        }

        [TestMethod]
        public void ReadTypeAnnotationSymbols_ZeroSymbol()
        {
            // an int with zero symbol annotation
            // $0::18
            var data = new byte[] { 0xE4, 0x81, 0x80, 0x21, 0x12 };
            var reader = new UserBinaryReader(new MemoryStream(data));

            ReaderTestCommon.ReadTypeAnnotationSymbols_ZeroSymbol(reader);
        }

        [TestMethod]
        public void ReadTypeAnnotationSymbols_AssertNoUnknownSymbolException()
        {
            byte[] data = DirStructure.OwnTestFileAsBytes("binary/unknown_symbols.bindat");
            UserBinaryReader reader = new UserBinaryReader(new MemoryStream(data));

            ReaderTestCommon.ReadTypeAnnotationSymbols_AssertNoUnknownSymbolException(reader);
        }

        [TestMethod]
        public void HasAnnotationTrue_SingleField()
        {
            // a singlefield structure with annotations
            // {withannot:years::months::days::hours::minutes::seconds::18}
            byte[] data = DirStructure.OwnTestFileAsBytes("binary/annot_singlefield.bindat");
            UserBinaryReader reader = new UserBinaryReader(new MemoryStream(data));

            ReaderTestCommon.HasAnnotationTrue_SingleField(reader);
        }

        [TestMethod]
        public void HasAnnotationFalse_SingleField()
        {
            // a singlefield structure with annotations
            // {withannot:years::months::days::hours::minutes::seconds::18}
            byte[] data = DirStructure.OwnTestFileAsBytes("binary/annot_singlefield.bindat");
            UserBinaryReader reader = new UserBinaryReader(new MemoryStream(data));

            ReaderTestCommon.HasAnnotationFalse_SingleField(reader);
        }

        [TestMethod]
        public void HasAnnotation_ZeroSymbol()
        {
            // an int with sannotation
            // $0::18
            var data = new byte[] { 0xE4, 0x81, 0x80, 0x21, 0x12 };
            var reader = new UserBinaryReader(new MemoryStream(data));

            ReaderTestCommon.HasAnnotation_ZeroSymbol(reader);
        }
        [TestMethod]
        public void HasAnnotation_AssertUnknownSymbolException()
        {
            byte[] data = DirStructure.OwnTestFileAsBytes("binary/unknown_symbols.bindat");
            UserBinaryReader reader = new UserBinaryReader(new MemoryStream(data));

            ReaderTestCommon.HasAnnotation_AssertUnknownSymbolException(reader);
        }

        [TestMethod]
        public void SingleSymbol()
        {
            //struct with single symbol
            //{single_symbol:'something'}
            var data = DirStructure.OwnTestFileAsBytes("binary/single_symbol.bindat");

            var reader = new UserBinaryReader(new MemoryStream(data));
            ReaderTestCommon.SingleSymbol(reader);
        }

        [TestMethod]
        public void SingleIntList()
        {
            var data = DirStructure.OwnTestFileAsBytes("binary/single_int_list.bindat");
            var reader = new UserBinaryReader(new MemoryStream(data));
            ReaderTestCommon.SingleIntList(reader);
        }

        /// <summary>
        /// Test for a typical json-style message
        /// </summary>
        [TestMethod]
        public void Combined1()
        {
            var data = DirStructure.OwnTestFileAsBytes("binary/combined1.bindat");
            var reader = new UserBinaryReader(new MemoryStream(data));

            ReaderTestCommon.Combined1(reader);
        }

        [TestMethod]
        public void Struct_OneBlob()
        {
            var data = DirStructure.OwnTestFileAsBytes("binary/struct_oneblob.bindat");
            var reader = new UserBinaryReader(new MemoryStream(data));
            ReaderTestCommon.Struct_OneBlob(reader);
        }

        [TestMethod]
        public void MultipleClobs()
        {
            IIonReader binReader;
            using (var ms = new MemoryStream())
            {
                ReadOnlySpan<byte> firstClob = new ReadOnlySpan<byte>(new byte[] { 97, 98, 99, 32, 100, 101 });
                ReadOnlySpan<byte> secondClob = new ReadOnlySpan<byte>(new byte[] { 65, 32, 66, 32, 67 });
                ReadOnlySpan<byte> thirdClob = new ReadOnlySpan<byte>(new byte[] { 50, 51, 32, 54, 55 });

                var binWriter = IonBinaryWriterBuilder.Build(ms);
                binWriter.WriteClob(firstClob);  // {{ "abc de" }}
                binWriter.WriteClob(secondClob); // {{ "A B\tC" }}
                binWriter.WriteClob(thirdClob);  // {{ "23 56" }}
                binWriter.Finish();

                ms.Seek(0, SeekOrigin.Begin);
                binReader = IonReaderBuilder.Build(ms);


                var type = binReader.MoveNext();
                var buffer = binReader.NewByteArray();
                Assert.AreEqual(IonType.Clob, type);
                Assert.AreEqual("abc de", Encoding.ASCII.GetString(buffer));

                type = binReader.MoveNext();
                 buffer = binReader.NewByteArray();
                Assert.AreEqual(IonType.Clob, type);
                Assert.AreEqual("A B C", Encoding.ASCII.GetString(buffer));

                type = binReader.MoveNext();
                 buffer = binReader.NewByteArray();
                Assert.AreEqual(IonType.Clob, type);
                Assert.AreEqual("23 67", Encoding.ASCII.GetString(buffer));
            }
        }


        [TestMethod]
        public void MultipleBlobs()
        {
            IIonReader binReader;
            using (var ms = new MemoryStream())
            {
                ReadOnlySpan<byte> firstBlob = new ReadOnlySpan<byte>(new byte[] { 68, 65, 72, 72, 75 });
                ReadOnlySpan<byte> secondBlob = new ReadOnlySpan<byte>(new byte[] { 68, 69 });
                ReadOnlySpan<byte> thirdBlob = new ReadOnlySpan<byte>(new byte[] { 68, 65, 85 });

                var binWriter = IonBinaryWriterBuilder.Build(ms);
                binWriter.WriteBlob(firstBlob);
                binWriter.WriteBlob(secondBlob);
                binWriter.WriteBlob(thirdBlob);
                binWriter.Finish();

                ms.Seek(0, SeekOrigin.Begin);
                binReader = IonReaderBuilder.Build(ms);


                var type = binReader.MoveNext();
                var buffer = binReader.NewByteArray();
                Assert.AreEqual(IonType.Blob, type);
                Assert.AreEqual("68 65 72 72 75 ", BytesToHex(buffer));

                type = binReader.MoveNext();
                buffer = binReader.NewByteArray();
                Assert.AreEqual(IonType.Blob, type);
                Assert.AreEqual("68 69 ", BytesToHex(buffer));

                type = binReader.MoveNext();
                buffer = binReader.NewByteArray();
                Assert.AreEqual(IonType.Blob, type);
                Assert.AreEqual("68 65 85 ", BytesToHex(buffer));
            }
        }

        /// <summary>
        /// Aims to test the correctness of skipping with step in-out in the middle
        /// of container
        /// </summary>
        [TestMethod]
        public void TwoLayer_TestStepout_Skip()
        {
            const string fileName = "binary/twolayer.bindat";
            var data = DirStructure.OwnTestFileAsBytes(fileName);
            var reader = new UserBinaryReader(new MemoryStream(data));
            ReaderTestCommon.TwoLayer_TestStepoutSkip(reader);
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

            var memStream = new MemoryStream();
            using (var writer = IonBinaryWriterBuilder.Build(memStream))
            {
                writer.WriteBlob(blob);
                writer.Finish();
            }
            var output = memStream.ToArray();
            var reader = IonReaderBuilder.Build(new MemoryStream(output));
            ReaderTestCommon.Blob_PartialRead(size, step, reader);
        }

        public string BytesToHex(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in bytes)
            {
                sb.Append(b.ToString("d2"));
                sb.Append(" ");
            }

            return sb.ToString();
        }
    }
}
