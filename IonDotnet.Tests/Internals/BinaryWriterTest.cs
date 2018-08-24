using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IonDotnet.Internals.Binary;
using IonDotnet.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Internals
{
    [TestClass]
    public class BinaryWriterTest
    {
        [TestMethod]
        public async Task WriteEmptyDatagram()
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new ManagedBinaryWriter(BinaryConstants.EmptySymbolTablesArray))
                {
                    await writer.FlushAsync(stream);
                    Assert.IsTrue(ReadUtils.Binary.DatagramEmpty(stream.ToArray()));
                }
            }
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void WriteSingleBool(bool val)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new ManagedBinaryWriter(BinaryConstants.EmptySymbolTablesArray))
                {
                    writer.WriteBool(val);
                    writer.FlushAsync(stream);
                    var bytes = stream.ToArray();
                    Assert.AreEqual(val, ReadUtils.Binary.ReadSingleBool(bytes));
                }
            }
        }

        [TestMethod]
        [DataRow("1.024")]
        [DataRow("-1.024")]
        [DataRow("3.01234567890123456789")]
        [DataRow("-3.01234567890123456789")]
        public void WriteSingleDecimal(string format)
        {
            var val = decimal.Parse(format);

            Console.WriteLine(val);

            using (var writer = new ManagedBinaryWriter(BinaryConstants.EmptySymbolTablesArray))
            {
                writer.WriteDecimal(val);
                byte[] bytes = null;
                writer.Flush(ref bytes);
                Assert.AreEqual(val, ReadUtils.Binary.ReadSingleDecimal(bytes));
            }
        }

        [TestMethod]
        public async Task WriteEmptyStruct()
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new ManagedBinaryWriter(BinaryConstants.EmptySymbolTablesArray))
                {
                    writer.StepIn(IonType.Struct);
                    writer.StepOut();
                    await writer.FlushAsync(stream);
                    var reader = new UserBinaryReader(new MemoryStream(stream.ToArray()));
                    Assert.IsTrue(reader.NextIsEmptyStruct());
                }
            }
        }

        /// <summary>
        /// Test for correct stepping in-out of structs
        /// </summary>
        [TestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(5)]
        [DataRow(10)]
        [DataRow(400)]
        [DataRow(1000)]
        public async Task WriteLayersDeep(int depth)
        {
            using (var stream = new MemoryStream())
            {
                List<(string key, object value)> kvps;
                using (var writer = new ManagedBinaryWriter(BinaryConstants.EmptySymbolTablesArray))
                {
                    writer.StepIn(IonType.Struct);
                    for (var i = 0; i < depth - 1; i++)
                    {
                        writer.SetFieldName($"layer{i}");
                        writer.StepIn(IonType.Struct);
                    }

                    kvps = WriteFlat(writer);

                    for (var i = 0; i < depth; i++)
                    {
                        writer.StepOut();
                    }

                    await writer.FlushAsync(stream);
                }

                var reader = new UserBinaryReader(new MemoryStream(stream.ToArray()));
                for (var i = 0; i < depth - 1; i++)
                {
                    Console.WriteLine(i);
                    Assert.AreEqual(IonType.Struct, reader.MoveNext());
                    Console.WriteLine(reader.CurrentFieldName);
                    reader.StepIn();
                }

                ReadUtils.AssertFlatStruct(reader, kvps);
            }
        }

        [TestMethod]
        public async Task WriteFlatStruct()
        {
            using (var stream = new MemoryStream())
            {
                List<(string key, object value)> kvps;
                using (var writer = new ManagedBinaryWriter(BinaryConstants.EmptySymbolTablesArray))
                {
                    writer.StepIn(IonType.Struct);

                    kvps = WriteFlat(writer);

                    writer.StepOut();
                    await writer.FlushAsync(stream);
                }

                var reader = new UserBinaryReader(new MemoryStream(stream.ToArray()));
                ReadUtils.AssertFlatStruct(reader, kvps);
            }
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(10)]
        [DataRow(50)]
        public async Task WriteObjectWithAnnotations(int annotationCount)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new ManagedBinaryWriter(BinaryConstants.EmptySymbolTablesArray))
                {
                    writer.StepIn(IonType.Struct);

                    writer.SetFieldName("FieldName");
                    for (var i = 0; i < annotationCount; i++)
                    {
                        writer.AddTypeAnnotation($"annot_{i}");
                    }

                    writer.WriteString("FieldValue");

                    writer.StepOut();
                    await writer.FlushAsync(stream);
                }

                var annotReader = new SaveAnnotationsReaderRoutine();
                var reader = new UserBinaryReader(new MemoryStream(stream.ToArray()), annotReader);
                reader.MoveNext();
                reader.StepIn();
                reader.MoveNext();
                //load the value
                reader.StringValue();
                for (var i = 0; i < annotationCount; i++)
                {
                    Assert.IsTrue(annotReader.Symbols.Contains($"annot_{i}"));
                }
            }
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(10)]
        [DataRow(50)]
        [DataRow(2000)]
        public async Task WriteStructWithSingleBlob(int blobSize)
        {
            var blob = new byte[blobSize];
            new Random().NextBytes(blob);
            using (var stream = new MemoryStream())
            {
                using (var writer = new ManagedBinaryWriter(BinaryConstants.EmptySymbolTablesArray))
                {
                    writer.StepIn(IonType.Struct);

                    writer.SetFieldName("blob");
                    writer.WriteBlob(blob);

                    writer.StepOut();
                    await writer.FlushAsync(stream);
                }

                var reader = new UserBinaryReader(new MemoryStream(stream.ToArray()));
                reader.MoveNext();
                reader.StepIn();
                Assert.AreEqual(IonType.Blob, reader.MoveNext());
                var size = reader.GetLobByteSize();
                Assert.AreEqual(blobSize, size);
                var readBlob = new byte[size];
                reader.GetBytes(readBlob);

                Assert.IsTrue(blob.SequenceEqual(readBlob));
            }
        }

        [TestMethod]
        public void WriteAnnotations_ExceedMaxSize()
        {
            void writeAlot(IIonWriter w)
            {
                w.StepIn(IonType.Struct);

                w.SetFieldName("FieldName");
                for (var i = 0; i < BinaryConstants.MaxAnnotationSize; i++)
                {
                    w.AddTypeAnnotation($"annot_{i}");
                }

                w.WriteString("FieldValue");

                w.StepOut();
            }

            IIonWriter writer;
            using (writer = new ManagedBinaryWriter(BinaryConstants.EmptySymbolTablesArray))
            {
                Assert.ThrowsException<IonException>(() => writeAlot(writer));
            }
        }

        [TestMethod]
        public async Task WriteNulls()
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new ManagedBinaryWriter(BinaryConstants.EmptySymbolTablesArray))
                {
                    writer.StepIn(IonType.Struct);

                    foreach (var iType in Enum.GetValues(typeof(IonType)))
                    {
                        if ((IonType) iType == IonType.Datagram || (IonType) iType == IonType.None) continue;

                        var name = Enum.GetName(typeof(IonType), iType);
                        writer.SetFieldName($"null_{name}");
                        writer.WriteNull((IonType) iType);
                    }

                    writer.StepOut();
                    await writer.FlushAsync(stream);
                }

                var reader = new UserBinaryReader(new MemoryStream(stream.ToArray()));
                reader.MoveNext();
                reader.StepIn();

                foreach (var iType in Enum.GetValues(typeof(IonType)))
                {
                    if ((IonType) iType == IonType.Datagram || (IonType) iType == IonType.None) continue;
                    var name = Enum.GetName(typeof(IonType), iType);
                    Assert.AreEqual((IonType) iType, reader.MoveNext());
                    Assert.AreEqual($"null_{name}", reader.CurrentFieldName);
                    Assert.IsTrue(reader.CurrentIsNull);
                }

                reader.StepOut();
            }
        }

        /// <summary>
        /// Just write a bunch of scalar values
        /// </summary>
        private static List<(string key, object value)> WriteFlat(IIonWriter writer)
        {
            var kvps = new List<(string key, object value)>();

            void writeAndAdd<T>(string fieldName, T value, Action<T> writeAction)
            {
                writer.SetFieldName(fieldName);
                writeAction(value);
                kvps.Add((fieldName, value));
            }

            writeAndAdd("boolean", true, writer.WriteBool);
            writeAndAdd("cstring", "somestring", writer.WriteString);
            writeAndAdd("int", 123456, i => writer.WriteInt(i));
            writeAndAdd("long", long.MaxValue / 10000, writer.WriteInt);
            writeAndAdd("datetime", new Timestamp(new DateTime(2000, 11, 11, 11, 11, 11, DateTimeKind.Utc)), writer.WriteTimestamp);
            writeAndAdd("decimal", 6.34233242123123123423m, writer.WriteDecimal);
            writeAndAdd("float", 231236.321312f, d => writer.WriteFloat(d));
            writeAndAdd("double", 231345.325667d * 133.346432d, writer.WriteFloat);

            return kvps;
        }
    }
}
