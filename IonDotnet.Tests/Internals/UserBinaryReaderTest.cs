using System.IO;
using System.Numerics;
using IonDotnet.Internals;
using IonDotnet.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Internals
{
    [TestClass]
    public class UserBinaryReaderTest
    {
        //simple datagram: {yolo:true}
        private static byte[] _oneBool;

        //a flat struct of scalar values:
        //boolean:true
        //str:yes
        //integer:123456
        //longInt:int.Max*2
        //bigInt:long.Max*10
        //double:2213.1267567f
        private static byte[] _flatScalar;

        //empty struct {}
        private static byte[] _trivial;

        [ClassInitialize]
        public static void Init(TestContext context)
        {
            _oneBool = DirStructure.ReadDataFile("onebool.bindat");
            _flatScalar = DirStructure.ReadDataFile("flat_scalar.bindat");
            _trivial = DirStructure.ReadDataFile("trivial.bindat");
        }

        [TestMethod]
        public void Trivial()
        {
            using (var reader = new UserBinaryReader(new MemoryStream(_trivial), new DefaultScalarConverter()))
            {
                reader.Next();
                Assert.AreEqual(IonType.Struct, reader.GetCurrentType());
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
            using (var reader = new UserBinaryReader(new MemoryStream(_oneBool), new DefaultScalarConverter()))
            {
                reader.Next();
                Assert.AreEqual(IonType.Struct, reader.GetCurrentType());
                reader.StepIn();
                Assert.AreEqual(1, reader.CurrentDepth);
                reader.Next();
                Assert.AreEqual(IonType.Bool, reader.GetCurrentType());
                Assert.AreEqual("yolo", reader.GetFieldName());
                Assert.AreEqual(true, reader.BoolValue());
                Assert.AreEqual(IonType.None, reader.Next());
                reader.StepOut();
                Assert.AreEqual(0, reader.CurrentDepth);
            }
        }

        [TestMethod]
        public void FlatScalar()
        {
            using (var reader = new UserBinaryReader(new MemoryStream(_flatScalar), new DefaultScalarConverter()))
            {
                reader.Next();
                Assert.AreEqual(IonType.Struct, reader.GetCurrentType());
                reader.StepIn();
                Assert.AreEqual(1, reader.CurrentDepth);

                reader.Next();
                Assert.AreEqual("boolean", reader.GetFieldName());
                Assert.AreEqual(IonType.Bool, reader.GetCurrentType());
                Assert.IsTrue(reader.BoolValue());

                reader.Next();
                Assert.AreEqual("str", reader.GetFieldName());
                Assert.AreEqual(IonType.String, reader.GetCurrentType());
                Assert.AreEqual("yes", reader.StringValue());

                reader.Next();
                Assert.AreEqual("integer", reader.GetFieldName());
                Assert.AreEqual(IonType.Int, reader.GetCurrentType());
                Assert.AreEqual(123456, reader.IntValue());

                reader.Next();
                Assert.AreEqual("longInt", reader.GetFieldName());
                Assert.AreEqual(IonType.Int, reader.GetCurrentType());
                Assert.AreEqual((long) int.MaxValue * 2, reader.LongValue());

                reader.Next();
                Assert.AreEqual("bigInt", reader.GetFieldName());
                Assert.AreEqual(IonType.Int, reader.GetCurrentType());
                Assert.AreEqual(BigInteger.Multiply(new BigInteger(long.MaxValue), 10), reader.BigIntegerValue());

                reader.Next();
                Assert.AreEqual("double", reader.GetFieldName());
                Assert.AreEqual(IonType.Float, reader.GetCurrentType());
                Assert.AreEqual(2213.1267567, reader.DoubleValue());

                Assert.AreEqual(IonType.None, reader.Next());
                reader.StepOut();
                Assert.AreEqual(0, reader.CurrentDepth);
            }
        }
    }
}
