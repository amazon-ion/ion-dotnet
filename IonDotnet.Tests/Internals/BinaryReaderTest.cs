using System;
using System.IO;
using IonDotnet.Internals.Binary;
using IonDotnet.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace IonDotnet.Tests.Internals
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
        [DataRow(new byte[] {0xE0, 0x01, 0x00, 0xEA, 0x11}, true)]
        [DataRow(new byte[] {0xE0, 0x01, 0x00, 0xEA, 0x10}, false)]
        public void SingleBool(byte[] data, bool value)
        {
            var reader = new UserBinaryReader(new MemoryStream(data));
            ReaderTestCommon.SingleBool(reader, value);
        }

        [TestMethod]
        [DataRow(new byte[] {0xE0, 0x01, 0x00, 0xEA, 0x24, 0x49, 0x96, 0x02, 0xD2}, 1234567890)]
        [DataRow(new byte[] {0xE0, 0x01, 0x00, 0xEA, 0x28, 0x3F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF}, long.MaxValue / 2)]
        public void SingleNumber(byte[] data, long value)
        {
            IIonReader reader = new UserBinaryReader(new MemoryStream(data));
            ReaderTestCommon.SingleNumber(reader, value);
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
        public void ReadAnnotations_SingleField()
        {
            // a singlefield structure with annotations
            // {withannot:years::months::days::hours::minutes::seconds::18}
            var annotSingleField = DirStructure.OwnTestFileAsBytes("binary/annot_singlefield.bindat");
            var reader = new UserBinaryReader(new MemoryStream(annotSingleField));

            ReaderTestCommon.ReadAnnotations_SingleField(reader);
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
    }
}
