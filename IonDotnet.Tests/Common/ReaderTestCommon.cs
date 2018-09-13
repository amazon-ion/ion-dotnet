using System;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Common
{
    public static class ReaderTestCommon
    {
        //empty content
        public static void Empty(IIonReader reader)
        {
            //also convieniently testing symtab
            Assert.IsNotNull(reader.GetSymbolTable());
            Assert.AreEqual(IonType.None, reader.MoveNext());
        }

        //empty struct {}
        public static void TrivialStruct(IIonReader reader)
        {
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
        public static void SingleBool(IIonReader reader, bool value)
        {
            Assert.AreEqual(IonType.Bool, reader.MoveNext());
            Assert.AreEqual(value, reader.BoolValue());
        }

        public static void SingleNumber(IIonReader reader, long value)
        {
            //a single number
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

        public static void OneBoolInStruct(IIonReader reader)
        {
            //simple datagram: {yolo:true}
            reader.MoveNext();
            Assert.AreEqual(IonType.Struct, reader.CurrentType);
            reader.StepIn();
            Assert.IsTrue(reader.IsInStruct);
            Assert.AreEqual(1, reader.CurrentDepth);
            reader.MoveNext();
            Assert.AreEqual(IonType.Bool, reader.CurrentType);
            Assert.AreEqual("yolo", reader.CurrentFieldName);
            Assert.AreEqual(true, reader.BoolValue());
            Assert.AreEqual(IonType.None, reader.MoveNext());
            reader.StepOut();
            Assert.AreEqual(0, reader.CurrentDepth);
        }

        public static void FlatScalar(IIonReader reader)
        {
            //a flat struct of scalar values:
            //boolean:true
            //str:"yes"
            //integer:123456
            //longInt:int.Max*2
            //bigInt:long.Max*10
            //double:2213.1267567f
            reader.MoveNext();
            Assert.AreEqual(IonType.Struct, reader.CurrentType);
            reader.StepIn();
            Assert.IsTrue(reader.IsInStruct);
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

        public static void FlatIntList(IIonReader reader)
        {
            //a flat list of ints [123,456,789]
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

        public static void ReadAnnotations_SingleField(IIonReader reader, SaveAnnotationsReaderRoutine converter)
        {
            // a singlefield structure with annotations
            // {withannot:years::months::days::hours::minutes::seconds::18}
            var symbols = new[] {"years", "months", "days", "hours", "minutes", "seconds"};

            reader.MoveNext();
            reader.StepIn();
            reader.MoveNext();
            Assert.AreEqual(IonType.Int, reader.CurrentType);
            Assert.AreEqual("withannot", reader.CurrentFieldName);
            Assert.AreEqual(18, reader.IntValue());
            Assert.IsTrue(symbols.SequenceEqual(converter.Symbols));

            foreach (var s in symbols)
            {
                Assert.IsTrue(converter.Symbols.Contains(s));
            }
        }

        public static void SingleSymbol(IIonReader reader)
        {
            //struct with single symbol
            //{single_symbol:'something'}
            reader.MoveNext();
            reader.StepIn();
            reader.MoveNext();
            Assert.AreEqual(IonType.Symbol, reader.CurrentType);
            Assert.AreEqual("single_symbol", reader.CurrentFieldName);
            Assert.AreEqual("something", reader.SymbolValue().Text);
        }

        public static void SingleIntList(IIonReader reader)
        {
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
        public static void Combined1(IIonReader reader)
        {
            Assert.AreEqual(IonType.Struct, reader.MoveNext());
            reader.StepIn();
            Assert.IsTrue(reader.IsInStruct);
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
            Assert.IsTrue(reader.IsInStruct);
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

        public static void Struct_OneBlob(IIonReader reader)
        {
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

        /// <remarks>See text/twolayer.ion for content</remarks>
        public static void TwoLayer_TestStepoutSkip(IIonReader reader)
        {
            Assert.AreEqual(IonType.Struct, reader.MoveNext());
            reader.StepIn();
            Assert.AreEqual(IonType.String, reader.MoveNext());
            Assert.AreEqual("open", reader.CurrentFieldName);

            Assert.AreEqual(IonType.Struct, reader.MoveNext());
            Assert.AreEqual("structure", reader.CurrentFieldName);
            reader.StepIn();

            Assert.AreEqual(IonType.String, reader.MoveNext());
            Assert.AreEqual("open", reader.CurrentFieldName);
            Assert.AreEqual(IonType.Struct, reader.MoveNext());
            Assert.AreEqual("structure", reader.CurrentFieldName);
            reader.StepIn();

            Assert.AreEqual(IonType.Int, reader.MoveNext());
            Assert.AreEqual("int", reader.CurrentFieldName);
            //1st skip
            reader.StepOut();

            Assert.AreEqual(IonType.Int, reader.MoveNext());
            Assert.AreEqual("int", reader.CurrentFieldName);
            //2nd skip
            reader.StepOut();

            Assert.AreEqual(IonType.String, reader.MoveNext());
            Assert.AreEqual("this is a string", reader.StringValue());
            Assert.AreEqual(IonType.Bool, reader.MoveNext());
            Assert.AreEqual(true, reader.BoolValue());
        }

        public static void AssertFloatEqual(double expected, double actual)
        {
            var sub = Math.Abs(expected - actual);
            var ok = sub <= double.Epsilon * 10;
            if (!ok)
            {
                Console.WriteLine($"e:{expected} - a:{actual} = {sub}");
            }

            Assert.IsTrue(ok);
        }
    }
}
