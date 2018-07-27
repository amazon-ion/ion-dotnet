using System;
using System.Collections.Generic;
using System.IO;
using IonDotnet.Internals;
using IonDotnet.Internals.Binary;
using IonDotnet.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Internals
{
    [TestClass]
    public class BinaryWriterTest
    {
        [TestMethod]
        public void WriteEmptyDatagram()
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new ManagedBinaryWriter(IonConstants.EmptySymbolTablesArray))
                {
                    writer.Flush(stream);
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
                using (var writer = new ManagedBinaryWriter(IonConstants.EmptySymbolTablesArray))
                {
                    writer.WriteBool(val);
                    writer.Flush(stream);
                    var bytes = stream.ToArray();
                    Assert.AreEqual(val, ReadUtils.Binary.ReadSingleBool(bytes));
                }
            }
        }

        [TestMethod]
        public void WriteEmptyStruct()
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new ManagedBinaryWriter(IonConstants.EmptySymbolTablesArray))
                {
                    writer.StepIn(IonType.Struct);
                    writer.StepOut();
                    writer.Flush(stream);
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
        [DataRow(1000)]
        public void WriteLayersDeep(int depth)
        {
            using (var stream = new MemoryStream())
            {
                List<(string key, object value)> kvps;
                using (var writer = new ManagedBinaryWriter(IonConstants.EmptySymbolTablesArray))
                {
                    for (var i = 0; i < depth; i++)
                    {
                        writer.StepIn(IonType.Struct);
                    }

                    kvps = WriteFlat(writer);

                    for (var i = 0; i < depth; i++)
                    {
                        writer.StepOut();
                    }

                    writer.StepOut();
                    writer.Flush(stream);
                }

                var reader = new UserBinaryReader(new MemoryStream(stream.ToArray()));
                for (var i = 0; i < depth - 1; i++)
                {
                    reader.StepIn();
                }

                ReadUtils.AssertFlatStruct(reader, kvps);
            }
        }

        [TestMethod]
        public void WriteFlatStruct()
        {
            using (var stream = new MemoryStream())
            {
                List<(string key, object value)> kvps;
                IIonWriter writer;
                using (writer = new ManagedBinaryWriter(IonConstants.EmptySymbolTablesArray))
                {
                    writer.StepIn(IonType.Struct);

                    kvps = WriteFlat(writer);

                    writer.StepOut();
                    writer.Flush(stream);
                }

                var reader = new UserBinaryReader(new MemoryStream(stream.ToArray()));
                ReadUtils.AssertFlatStruct(reader, kvps);
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
            writeAndAdd("datetime", new DateTime(2000, 11, 11, 11, 11, 11, DateTimeKind.Utc), writer.WriteTimestamp);
            writeAndAdd("decimal", 6.34233242123123123423m, writer.WriteDecimal);
            writeAndAdd("float", 231236.321312f, d => writer.WriteFloat(d));
            writeAndAdd("double", 231345.325667d * 133.346432d, writer.WriteFloat);
            return kvps;
        }
    }
}
