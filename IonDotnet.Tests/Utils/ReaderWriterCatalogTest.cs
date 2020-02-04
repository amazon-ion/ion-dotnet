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
using IonDotnet.Builders;
using IonDotnet.Internals;
using IonDotnet.Internals.Binary;
using IonDotnet.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Utils
{
    [TestClass]
    public class ReaderWriterCatalogTest
    {
        [TestMethod]
        public void WriterReader()
        {
            var catalog = new SimpleCatalog();
            var table1 = SharedSymbolTable.NewSharedSymbolTable("table1", 1, null, new[] {"s1", "s2"});
            var table2 = SharedSymbolTable.NewSharedSymbolTable("table2", 1, null, new[] {"s3", "s4"});

            catalog.PutTable(table1);
            catalog.PutTable(table2);

            var stream = new MemoryStream();
            byte[] output;
            using (var binWriter = new ManagedBinaryWriter(stream, new[] {table1, table2}))
            {
                binWriter.StepIn(IonType.Struct);
                binWriter.SetFieldName("s1");
                binWriter.WriteSymbol("s2");
                binWriter.SetFieldName("s3");
                binWriter.WriteSymbol("s4");
                binWriter.StepOut();
                binWriter.Finish();
                Assert.AreEqual(binWriter.SymbolTable.GetImportedMaxId(), binWriter.SymbolTable.MaxId);

                output = stream.ToArray();
            }

            var reader = IonReaderBuilder.Build(new MemoryStream(output));
            Assert.AreEqual(IonType.Struct, reader.MoveNext());
            reader.StepIn();
            Assert.AreEqual(IonType.Symbol, reader.MoveNext());
            var fns = reader.GetFieldNameSymbol();
            Assert.AreEqual(10, fns.Sid);
            Assert.IsNull(fns.Text);
            Assert.ThrowsException<UnknownSymbolException>(() => reader.CurrentFieldName);
            Assert.AreEqual(11, reader.SymbolValue().Sid);

            Assert.AreEqual(IonType.Symbol, reader.MoveNext());
            fns = reader.GetFieldNameSymbol();
            Assert.AreEqual(12, fns.Sid);
            Assert.IsNull(fns.Text);
            Assert.ThrowsException<UnknownSymbolException>(() => reader.CurrentFieldName);
            Assert.AreEqual(13, reader.SymbolValue().Sid);

            //make sure that a reader with the correct imports can read it
            var reader2 = new UserBinaryReader(new MemoryStream(output), catalog);
            Assert.AreEqual(IonType.Struct, reader2.MoveNext());
            var localTable = reader2.GetSymbolTable();
            Console.WriteLine(localTable.Find("s1").ToString());
            reader2.StepIn();
            Assert.AreEqual(IonType.Symbol, reader2.MoveNext());
            Assert.AreEqual("s1", reader2.CurrentFieldName);
            Assert.AreEqual("s2", reader2.SymbolValue().Text);

            Assert.AreEqual(IonType.Symbol, reader2.MoveNext());
            Assert.AreEqual("s3", reader2.CurrentFieldName);
            Assert.AreEqual("s4", reader2.SymbolValue().Text);
        }
    }
}
