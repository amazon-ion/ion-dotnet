using System.IO;
using System.Linq;
using System.Numerics;
using IonDotnet.Internals;
using IonDotnet.Internals.Binary;
using IonDotnet.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Internals
{
    [TestClass]
    public class UserBinaryReaderTest
    {
        [TestMethod]
        public void Trivial()
        {
            //empty struct {}
            var trivial = DirStructure.ReadDataFile("trivial.bindat");

            using (var reader = new UserBinaryReader(new MemoryStream(trivial)))
            {
                reader.Next();
                Assert.AreEqual(IonType.Struct, reader.CurrentType);
                reader.StepIn();
                Assert.AreEqual(1, reader.CurrentDepth);
                Assert.AreEqual(IonType.None, reader.Next());
                for (var i = 0; i < 10; i++)
                {
                    Assert.AreEqual(IonType.None, reader.Next());
                }

                reader.StepOut();
                Assert.AreEqual(0, reader.CurrentDepth);
            }
        }

        [TestMethod]
        public void OneBool()
        {
            //simple datagram: {yolo:true}
            var oneBool = DirStructure.ReadDataFile("onebool.bindat");
            using (var reader = new UserBinaryReader(new MemoryStream(oneBool)))
            {
                reader.Next();
                Assert.AreEqual(IonType.Struct, reader.CurrentType);
                reader.StepIn();
                Assert.AreEqual(1, reader.CurrentDepth);
                reader.Next();
                Assert.AreEqual(IonType.Bool, reader.CurrentType);
                Assert.AreEqual("yolo", reader.CurrentFieldName);
                Assert.AreEqual(true, reader.BoolValue());
                Assert.AreEqual(IonType.None, reader.Next());
                reader.StepOut();
                Assert.AreEqual(0, reader.CurrentDepth);
            }
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

            using (var reader = new UserBinaryReader(new MemoryStream(flatScalar)))
            {
                reader.Next();
                Assert.AreEqual(IonType.Struct, reader.CurrentType);
                reader.StepIn();
                Assert.AreEqual(1, reader.CurrentDepth);

                reader.Next();
                Assert.AreEqual("boolean", reader.CurrentFieldName);
                Assert.AreEqual(IonType.Bool, reader.CurrentType);
                Assert.IsTrue(reader.BoolValue());

                reader.Next();
                Assert.AreEqual("str", reader.CurrentFieldName);
                Assert.AreEqual(IonType.String, reader.CurrentType);
                Assert.AreEqual("yes", reader.StringValue());

                reader.Next();
                Assert.AreEqual("integer", reader.CurrentFieldName);
                Assert.AreEqual(IonType.Int, reader.CurrentType);
                Assert.AreEqual(123456, reader.IntValue());

                reader.Next();
                Assert.AreEqual("longInt", reader.CurrentFieldName);
                Assert.AreEqual(IonType.Int, reader.CurrentType);
                Assert.AreEqual((long) int.MaxValue * 2, reader.LongValue());

                reader.Next();
                Assert.AreEqual("bigInt", reader.CurrentFieldName);
                Assert.AreEqual(IonType.Int, reader.CurrentType);
                Assert.AreEqual(BigInteger.Multiply(new BigInteger(long.MaxValue), 10), reader.BigIntegerValue());

                reader.Next();
                Assert.AreEqual("double", reader.CurrentFieldName);
                Assert.AreEqual(IonType.Float, reader.CurrentType);
                Assert.AreEqual(2213.1267567, reader.DoubleValue());

                Assert.AreEqual(IonType.None, reader.Next());
                reader.StepOut();
                Assert.AreEqual(0, reader.CurrentDepth);
            }
        }

        [TestMethod]
        public void FlatIntList()
        {
            //a flat list of ints [123,456,789]
            var flatListInt = DirStructure.ReadDataFile("flatlist_int.bindat");

            using (var reader = new UserBinaryReader(new MemoryStream(flatListInt)))
            {
                reader.Next();
                Assert.AreEqual(IonType.List, reader.CurrentType);
                reader.StepIn();
                Assert.AreEqual(1, reader.CurrentDepth);

                reader.Next();
                Assert.AreEqual(IonType.Int, reader.CurrentType);
                Assert.AreEqual(123, reader.IntValue());

                reader.Next();
                Assert.AreEqual(IonType.Int, reader.CurrentType);
                Assert.AreEqual(456, reader.IntValue());

                reader.Next();
                Assert.AreEqual(IonType.Int, reader.CurrentType);
                Assert.AreEqual(789, reader.IntValue());

                Assert.AreEqual(IonType.None, reader.Next());
                reader.StepOut();
                Assert.AreEqual(0, reader.CurrentDepth);
            }
        }

        [TestMethod]
        public void ReadAnnotations_SingleField()
        {
            // a singlefield structure with annotations
            // {withannot:years::months::days::hours::minutes::seconds::18}
            var annotSingleField = DirStructure.ReadDataFile("annot_singlefield.bindat");

            var symbols = new[] {"years", "months", "days", "hours", "minutes", "seconds"};
            var converter = new SaveAnnotationsConverter();
            using (var reader = new UserBinaryReader(new MemoryStream(annotSingleField), converter))
            {
                reader.Next();
                reader.StepIn();
                reader.Next();
                Assert.AreEqual(IonType.Int, reader.CurrentType);
                Assert.AreEqual("withannot", reader.CurrentFieldName);
                Assert.AreEqual(18, reader.IntValue());
                Assert.IsTrue(symbols.SequenceEqual(converter.Symbols));
            }
        }

        [TestMethod]
        public void SingleSymbol()
        {
            //struct with single symbol
            //{single_symbol:'something'}
            var data = DirStructure.ReadDataFile("single_symbol.bindat");

            using (var reader = new UserBinaryReader(new MemoryStream(data)))
            {
                reader.Next();
                reader.StepIn();
                reader.Next();
                Assert.AreEqual(IonType.Symbol, reader.CurrentType);
                Assert.AreEqual("single_symbol", reader.CurrentFieldName);
                Assert.AreEqual("something", reader.StringValue());
                var expectedToken = reader.GetSymbolTable().Find("something");
                Assert.AreEqual(expectedToken, reader.SymbolValue());
            }
        }
    }
}
