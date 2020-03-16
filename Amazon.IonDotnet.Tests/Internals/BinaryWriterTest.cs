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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.IonDotnet.Builders;
using Amazon.IonDotnet.Internals.Binary;
using Amazon.IonDotnet.Tests.Common;
using Amazon.IonDotnet.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.IonDotnet.Tests.Internals
{
    [TestClass]
    public class BinaryWriterTest
    {
        private MemoryStream _memoryStream;

        [TestInitialize]
        public void Initialize()
        {
            _memoryStream = new MemoryStream();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _memoryStream.Dispose();
        }

        [TestMethod]
        public void WriteEmptyDatagram()
        {
            using (var writer = new ManagedBinaryWriter(_memoryStream, Symbols.EmptySymbolTablesArray))
            {
                writer.Flush();
                Assert.IsTrue(ReadUtils.Binary.DatagramEmpty(_memoryStream.GetWrittenBuffer()));
            }
        }

        /// <summary>
        /// Test that multiple calls to <see cref="IIonWriter.Flush"/> does not write any extra bits.
        /// </summary>
        /// <param name="count">Number of calls to flush()</param>
        [TestMethod]
        [DataRow(1)]
        [DataRow(5)]
        [DataRow(50)]
        public void MultipleFlushes_SameOutput(int count)
        {
            using (var writer = new ManagedBinaryWriter(_memoryStream, Symbols.EmptySymbolTablesArray))
            {
                writer.StepIn(IonType.Struct);
                writer.SetFieldName("key");
                writer.WriteString("value");
                writer.StepOut();

                writer.Flush();
                var size = _memoryStream.Length;
                for (var i = 0; i < count; i++)
                {
                    writer.Flush();
                    Assert.AreEqual(size, _memoryStream.Length);
                }

                writer.Finish();
                Assert.AreEqual(size, _memoryStream.Length);

                _memoryStream.Seek(0, SeekOrigin.Begin);
                var reader = new UserBinaryReader(_memoryStream);
                Assert.AreEqual(IonType.Struct, reader.MoveNext());
                reader.StepIn();
                Assert.AreEqual(IonType.String, reader.MoveNext());
                Assert.AreEqual("key", reader.CurrentFieldName);
                Assert.AreEqual("value", reader.StringValue());
                Assert.AreEqual(IonType.None, reader.MoveNext());
                reader.StepOut();
                Assert.AreEqual(IonType.None, reader.MoveNext());
            }
        }

        /// <summary>
        /// Test that calling flush() after finish() does not write any extra bits except Bvm
        /// </summary>
        [TestMethod]
        public void FlushAfterFinish()
        {
            using (var writer = new ManagedBinaryWriter(_memoryStream, Symbols.EmptySymbolTablesArray))
            {
                writer.StepIn(IonType.Struct);
                writer.SetFieldName("key");
                writer.WriteString("value");
                writer.StepOut();

                writer.Finish();
                var size = _memoryStream.Length;

                writer.Flush();
                Assert.AreEqual(size + BinaryConstants.BinaryVersionMarkerLength, _memoryStream.Length);

                _memoryStream.Seek(0, SeekOrigin.Begin);
                var reader = new UserBinaryReader(_memoryStream);
                Assert.AreEqual(IonType.Struct, reader.MoveNext());
                reader.StepIn();
                Assert.AreEqual(IonType.String, reader.MoveNext());
                Assert.AreEqual("key", reader.CurrentFieldName);
                Assert.AreEqual("value", reader.StringValue());
                Assert.AreEqual(IonType.None, reader.MoveNext());
                reader.StepOut();
                //movenext() should skip over bvm
                Assert.AreEqual(IonType.None, reader.MoveNext());
            }
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void WriteSingleBool(bool val)
        {
            using (var writer = new ManagedBinaryWriter(_memoryStream, Symbols.EmptySymbolTablesArray))
            {
                writer.WriteBool(val);
                writer.Flush();
                var bytes = _memoryStream.GetWrittenBuffer();
                Assert.AreEqual(val, ReadUtils.Binary.ReadSingleBool(bytes));
            }
        }

        [TestMethod]
        [DataRow("1.024")]
        [DataRow("-1.024")]
        [DataRow("3.01234567890123456789")]
        [DataRow("-3.01234567890123456789")]
        public void WriteSingleDecimal(string format)
        {
            var val = decimal.Parse(format, System.Globalization.CultureInfo.InvariantCulture);

            Console.WriteLine(val);

            using (var writer = new ManagedBinaryWriter(_memoryStream, Symbols.EmptySymbolTablesArray))
            {
                writer.WriteDecimal(val);
                writer.Flush();
                var bytes = _memoryStream.GetWrittenBuffer();
                Assert.AreEqual(val, ReadUtils.Binary.ReadSingleDecimal(bytes).ToDecimal());
            }
        }

        [TestMethod]
        public void WriteEmptyStruct()
        {
            using (var writer = new ManagedBinaryWriter(_memoryStream, Symbols.EmptySymbolTablesArray))
            {
                writer.StepIn(IonType.Struct);
                writer.StepOut();
                writer.Flush();
                var reader = new UserBinaryReader(new MemoryStream(_memoryStream.GetWrittenBuffer()));
                Assert.IsTrue(reader.NextIsEmptyStruct());
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
        public void WriteLayersDeep(int depth)
        {
            List<(string key, object value)> kvps;
            using (var writer = new ManagedBinaryWriter(_memoryStream, Symbols.EmptySymbolTablesArray))
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

                writer.Flush();
            }

            var reader = new UserBinaryReader(new MemoryStream(_memoryStream.GetWrittenBuffer()));
            for (var i = 0; i < depth - 1; i++)
            {
                Console.WriteLine(i);
                Assert.AreEqual(IonType.Struct, reader.MoveNext());
                Console.WriteLine(reader.CurrentFieldName);
                reader.StepIn();
            }

            ReadUtils.AssertFlatStruct(reader, kvps);
        }

        [TestMethod]
        public void WriteFlatStruct()
        {
            List<(string key, object value)> kvps;
            using (var writer = new ManagedBinaryWriter(_memoryStream, Symbols.EmptySymbolTablesArray))
            {
                writer.StepIn(IonType.Struct);

                kvps = WriteFlat(writer);

                writer.StepOut();
                writer.Flush();
            }

            var reader = new UserBinaryReader(new MemoryStream(_memoryStream.GetWrittenBuffer()));
            ReadUtils.AssertFlatStruct(reader, kvps);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(10)]
        [DataRow(50)]
        public void WriteObjectWithAnnotations(int annotationCount)
        {
            using (var writer = new ManagedBinaryWriter(_memoryStream, Symbols.EmptySymbolTablesArray))
            {
                writer.StepIn(IonType.Struct);

                writer.SetFieldName("FieldName");
                for (var i = 0; i < annotationCount; i++)
                {
                    writer.AddTypeAnnotation($"annot_{i}");
                }

                writer.WriteString("FieldValue");

                writer.StepOut();
                writer.Flush();
            }

            var reader = new UserBinaryReader(new MemoryStream(_memoryStream.GetWrittenBuffer()));
            reader.MoveNext();
            reader.StepIn();
            reader.MoveNext();

            // Load the value.
            reader.StringValue();

            var annotations = reader.GetTypeAnnotationSymbols().ToList();

            Assert.AreEqual(annotations.Count, annotationCount);

            for (var i = 0; i < annotationCount; i++)
            {
                Assert.IsTrue(annotations.Any(s => s.Text == $"annot_{i}"));
            }
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(10)]
        [DataRow(50)]
        [DataRow(2000)]
        public void WriteStructWithSingleBlob(int blobSize)
        {
            var blob = new byte[blobSize];
            new Random().NextBytes(blob);
            using (var writer = new ManagedBinaryWriter(_memoryStream, Symbols.EmptySymbolTablesArray))
            {
                writer.StepIn(IonType.Struct);

                writer.SetFieldName("blob");
                writer.WriteBlob(blob);

                writer.StepOut();
                writer.Flush();
            }

            var reader = new UserBinaryReader(new MemoryStream(_memoryStream.GetWrittenBuffer()));
            reader.MoveNext();
            reader.StepIn();
            Assert.AreEqual(IonType.Blob, reader.MoveNext());
            var size = reader.GetLobByteSize();
            Assert.AreEqual(blobSize, size);
            var readBlob = new byte[size];
            reader.GetBytes(readBlob);

            Assert.IsTrue(blob.SequenceEqual(readBlob));
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
            using (writer = new ManagedBinaryWriter(_memoryStream, Symbols.EmptySymbolTablesArray))
            {
                Assert.ThrowsException<IonException>(() => writeAlot(writer));
            }
        }

        [TestMethod]
        public void WriteNulls()
        {
            using (var writer = new ManagedBinaryWriter(_memoryStream, Symbols.EmptySymbolTablesArray))
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
                writer.Flush();
            }

            var reader = new UserBinaryReader(new MemoryStream(_memoryStream.GetWrittenBuffer()));
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

        [TestMethod]
        [DataRow(2e0)]
        public void WriteFloatWithForceFloatEnabledAndDisabled(double val)
        {
            int array32BitLength = 0;
            int array64BitLength = 0;
            using (var writer = new ManagedBinaryWriter(_memoryStream, Symbols.EmptySymbolTablesArray, forceFloat64: false))
            {
                writer.WriteFloat(val);
                writer.Flush();
                array32BitLength = _memoryStream.GetWrittenBuffer().Length;

                _memoryStream.SetLength(0);
            }

            using (var writer = new ManagedBinaryWriter(_memoryStream, Symbols.EmptySymbolTablesArray, forceFloat64 : true))
            {
                writer.WriteFloat(val);
                writer.Flush();
                array64BitLength = _memoryStream.GetWrittenBuffer().Length;

            }

            Assert.AreEqual(array32BitLength + 4, array64BitLength);
        }

        [TestMethod]
        [DataRow("[1, 2, 3]", IonType.List)]
        [DataRow("(1 2 3)", IonType.Sexp)]
        [DataRow("{a:3}", IonType.Struct)]
        public void ContainerTypesInWriter(string value, IonType expectedType)
        {
            var reader = IonReaderBuilder.Build(value);
            reader.MoveNext();

            MemoryStream memoryStream = new MemoryStream();
            var writer = IonBinaryWriterBuilder.Build(memoryStream);
            writer.StepIn(IonType.Struct);
            writer.SetFieldName("fieldName");
            writer.WriteValue(reader);
            writer.StepOut();
            writer.Finish();

            memoryStream.Flush();
            var bytes = memoryStream.ToArray();

            reader = IonReaderBuilder.Build(bytes);
            reader.MoveNext();
            reader.StepIn();
            var actualType = reader.MoveNext();

            Assert.AreEqual(expectedType, actualType);
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
