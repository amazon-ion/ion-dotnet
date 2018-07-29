using System;
using System.IO;
using System.Linq;
using System.Numerics;
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
            IIonReader reader = new UserBinaryReader(new MemoryStream(bytes));
            Assert.AreEqual(IonType.None, reader.MoveNext());
        }

        [TestMethod]
        public void TrivialStruct()
        {
            //empty struct {}
            var trivial = DirStructure.ReadDataFile("trivial.bindat");
            IIonReader reader = new UserBinaryReader(new MemoryStream(trivial));
            reader.MoveNext();
            Assert.AreEqual(IonType.Struct, reader.CurrentType);
            reader.StepIn();
            Assert.AreEqual(1, reader.CurrentDepth);
            Assert.AreEqual(IonType.None, reader.MoveNext());
            for (var i = 0; i < 10; i++)
            {
                Assert.AreEqual(IonType.None, reader.MoveNext());
            }

            reader.StepOut();
            Assert.AreEqual(0, reader.CurrentDepth);
        }

        /// <summary>
        /// Test for single-value bool 
        /// </summary>
        [TestMethod]
        [DataRow(new byte[] {0xE0, 0x01, 0x00, 0xEA, 0x11}, true)]
        [DataRow(new byte[] {0xE0, 0x01, 0x00, 0xEA, 0x10}, false)]
        public void SingleBool(byte[] data, bool value)
        {
            IIonReader reader = new UserBinaryReader(new MemoryStream(data));
            Assert.AreEqual(IonType.Bool, reader.MoveNext());
            Assert.AreEqual(value, reader.BoolValue());
        }

        [TestMethod]
        [DataRow(new byte[] {0xE0, 0x01, 0x00, 0xEA, 0x24, 0x49, 0x96, 0x02, 0xD2}, 1234567890)]
        [DataRow(new byte[] {0xE0, 0x01, 0x00, 0xEA, 0x28, 0x3F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF}, long.MaxValue / 2)]
        public void SingleNumber(byte[] data, long value)
        {
            IIonReader reader = new UserBinaryReader(new MemoryStream(data));
            Assert.AreEqual(IonType.Int, reader.MoveNext());
            switch (reader.GetIntegerSize())
            {
                case IntegerSize.Unknown:
                    break;
                case IntegerSize.Int:
                    Assert.AreEqual(value, reader.IntValue());
                    break;
                case IntegerSize.Long:
                    Assert.AreEqual(value, reader.LongValue());
                    break;
                case IntegerSize.BigInteger:
                    Assert.Fail("not testing big int");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        [TestMethod]
        public void OneBoolInStruct()
        {
            //simple datagram: {yolo:true}
            var oneBool = DirStructure.ReadDataFile("onebool.bindat");
            var reader = new UserBinaryReader(new MemoryStream(oneBool));
            reader.MoveNext();
            Assert.AreEqual(IonType.Struct, reader.CurrentType);
            reader.StepIn();
            Assert.AreEqual(1, reader.CurrentDepth);
            reader.MoveNext();
            Assert.AreEqual(IonType.Bool, reader.CurrentType);
            Assert.AreEqual("yolo", reader.CurrentFieldName);
            Assert.AreEqual(true, reader.BoolValue());
            Assert.AreEqual(IonType.None, reader.MoveNext());
            reader.StepOut();
            Assert.AreEqual(0, reader.CurrentDepth);
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
            var flatScalar = DirStructure.ReadDataFile("flat_scalar.bindat");

            var reader = new UserBinaryReader(new MemoryStream(flatScalar));
            reader.MoveNext();
            Assert.AreEqual(IonType.Struct, reader.CurrentType);
            reader.StepIn();
            Assert.AreEqual(1, reader.CurrentDepth);

            reader.MoveNext();
            Assert.AreEqual("boolean", reader.CurrentFieldName);
            Assert.AreEqual(IonType.Bool, reader.CurrentType);
            Assert.IsTrue(reader.BoolValue());

            reader.MoveNext();
            Assert.AreEqual("str", reader.CurrentFieldName);
            Assert.AreEqual(IonType.String, reader.CurrentType);
            Assert.AreEqual("yes", reader.StringValue());

            reader.MoveNext();
            Assert.AreEqual("integer", reader.CurrentFieldName);
            Assert.AreEqual(IonType.Int, reader.CurrentType);
            Assert.AreEqual(123456, reader.IntValue());

            reader.MoveNext();
            Assert.AreEqual("longInt", reader.CurrentFieldName);
            Assert.AreEqual(IonType.Int, reader.CurrentType);
            Assert.AreEqual((long) int.MaxValue * 2, reader.LongValue());

            reader.MoveNext();
            Assert.AreEqual("bigInt", reader.CurrentFieldName);
            Assert.AreEqual(IonType.Int, reader.CurrentType);
            Assert.AreEqual(BigInteger.Multiply(new BigInteger(long.MaxValue), 10), reader.BigIntegerValue());

            reader.MoveNext();
            Assert.AreEqual("double", reader.CurrentFieldName);
            Assert.AreEqual(IonType.Float, reader.CurrentType);
            Assert.AreEqual(2213.1267567, reader.DoubleValue());

            Assert.AreEqual(IonType.None, reader.MoveNext());
            reader.StepOut();
            Assert.AreEqual(0, reader.CurrentDepth);
        }

        [TestMethod]
        public void FlatIntList()
        {
            //a flat list of ints [123,456,789]
            var flatListInt = DirStructure.ReadDataFile("flatlist_int.bindat");

            var reader = new UserBinaryReader(new MemoryStream(flatListInt));
            reader.MoveNext();
            Assert.AreEqual(IonType.List, reader.CurrentType);
            reader.StepIn();
            Assert.AreEqual(1, reader.CurrentDepth);

            reader.MoveNext();
            Assert.AreEqual(IonType.Int, reader.CurrentType);
            Assert.AreEqual(123, reader.IntValue());

            reader.MoveNext();
            Assert.AreEqual(IonType.Int, reader.CurrentType);
            Assert.AreEqual(456, reader.IntValue());

            reader.MoveNext();
            Assert.AreEqual(IonType.Int, reader.CurrentType);
            Assert.AreEqual(789, reader.IntValue());

            Assert.AreEqual(IonType.None, reader.MoveNext());
            reader.StepOut();
            Assert.AreEqual(0, reader.CurrentDepth);
        }

        [TestMethod]
        public void ReadAnnotations_SingleField()
        {
            // a singlefield structure with annotations
            // {withannot:years::months::days::hours::minutes::seconds::18}
            var annotSingleField = DirStructure.ReadDataFile("annot_singlefield.bindat");

            var symbols = new[] {"years", "months", "days", "hours", "minutes", "seconds"};
            var converter = new SaveAnnotationsReaderRoutine();
            var reader = new UserBinaryReader(new MemoryStream(annotSingleField), converter);

            reader.MoveNext();
            reader.StepIn();
            reader.MoveNext();
            Assert.AreEqual(IonType.Int, reader.CurrentType);
            Assert.AreEqual("withannot", reader.CurrentFieldName);
            Assert.AreEqual(18, reader.IntValue());
            Assert.IsTrue(symbols.SequenceEqual(converter.Symbols));
        }

        [TestMethod]
        public void SingleSymbol()
        {
            //struct with single symbol
            //{single_symbol:'something'}
            var data = DirStructure.ReadDataFile("single_symbol.bindat");

            var reader = new UserBinaryReader(new MemoryStream(data));
            reader.MoveNext();
            reader.StepIn();
            reader.MoveNext();
            Assert.AreEqual(IonType.Symbol, reader.CurrentType);
            Assert.AreEqual("single_symbol", reader.CurrentFieldName);
            Assert.AreEqual("something", reader.StringValue());
            var expectedToken = reader.GetSymbolTable().Find("something");
            Assert.AreEqual(expectedToken, reader.SymbolValue());
        }

        [TestMethod]
        public void SingleIntList()
        {
            var data = DirStructure.ReadDataFile("single_int_list.bindat");
            var reader = new UserBinaryReader(new MemoryStream(data));
            Assert.AreEqual(IonType.List, reader.MoveNext());
            reader.StepIn();

            Assert.AreEqual(IonType.Int, reader.MoveNext());
            Assert.AreEqual(1234, reader.IntValue());
            Assert.AreEqual(IonType.Int, reader.MoveNext());
            Assert.AreEqual(5678, reader.IntValue());
            Assert.AreEqual(IonType.Int, reader.MoveNext());
            Assert.AreEqual(6421, reader.IntValue());
            Assert.AreEqual(IonType.Int, reader.MoveNext());
            Assert.AreEqual(int.MinValue, reader.IntValue());
            Assert.AreEqual(IonType.Int, reader.MoveNext());
            Assert.AreEqual(int.MaxValue, reader.IntValue());
        }

        /// <summary>
        /// Test for a typical json-style message
        /// </summary>
        [TestMethod]
        public void Combined1()
        {
            var data = DirStructure.ReadDataFile("combined1.bindat");
            var reader = new UserBinaryReader(new MemoryStream(data));

            Assert.AreEqual(IonType.Struct, reader.MoveNext());
            reader.StepIn();
            Assert.AreEqual(IonType.Struct, reader.MoveNext());
            Assert.AreEqual("menu", reader.CurrentFieldName);
            reader.StepIn();
            Assert.AreEqual(IonType.String, reader.MoveNext());
            Assert.AreEqual("id", reader.CurrentFieldName);
            Assert.AreEqual("file", reader.StringValue());
            Assert.AreEqual(IonType.List, reader.MoveNext());
            Assert.AreEqual("popup", reader.CurrentFieldName);
            reader.StepIn();
            Assert.AreEqual(IonType.String, reader.MoveNext());
            Assert.AreEqual("Open", reader.StringValue());
            Assert.AreEqual(IonType.String, reader.MoveNext());
            Assert.AreEqual("Load", reader.StringValue());
            Assert.AreEqual(IonType.String, reader.MoveNext());
            Assert.AreEqual("Close", reader.StringValue());
            reader.StepOut();

            Assert.AreEqual(IonType.Struct, reader.MoveNext());
            Assert.AreEqual("deep1", reader.CurrentFieldName);
            reader.StepIn();
            Assert.AreEqual(IonType.Struct, reader.MoveNext());
            Assert.AreEqual("deep2", reader.CurrentFieldName);
            reader.StepIn();
            Assert.AreEqual(IonType.Struct, reader.MoveNext());
            Assert.AreEqual("deep3", reader.CurrentFieldName);
            reader.StepIn();
            Assert.AreEqual(IonType.String, reader.MoveNext());
            Assert.AreEqual("deep4val", reader.CurrentFieldName);
            Assert.AreEqual("enddeep", reader.StringValue());
            reader.StepOut();
            reader.StepOut();
            reader.StepOut();

            Assert.AreEqual(IonType.List, reader.MoveNext());
            Assert.AreEqual("positions", reader.CurrentFieldName);
            reader.StepIn();
            Assert.AreEqual(IonType.Int, reader.MoveNext());
            Assert.AreEqual(1234, reader.IntValue());
            Assert.AreEqual(IonType.Int, reader.MoveNext());
            Assert.AreEqual(5678, reader.IntValue());
            Assert.AreEqual(IonType.Int, reader.MoveNext());
            Assert.AreEqual(90, reader.IntValue());
            reader.StepOut();
            reader.StepOut();
            reader.StepOut();

            Assert.AreEqual(0, reader.CurrentDepth);
        }

        [TestMethod]
        public void Struct_OneBlob()
        {
            var data = DirStructure.ReadDataFile("struct_oneblob.bindat");
            var reader = new UserBinaryReader(new MemoryStream(data));
            reader.MoveNext();
            reader.StepIn();
            Assert.AreEqual(IonType.Blob, reader.MoveNext());
            Assert.AreEqual("blobbbb", reader.CurrentFieldName);
            var lobByteSize = reader.GetLobByteSize();
            Assert.AreEqual(100, lobByteSize);
            var blob = new byte[lobByteSize];
            reader.GetBytes(blob);
            
            for (var i = 0; i < 100; i++)
            {
                Assert.AreEqual((byte) 1, blob[i]);
            }
        }
    }
}
