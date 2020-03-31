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
using Amazon.IonDotnet.Internals;
using Amazon.IonDotnet.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.IonDotnet.Tests.Internals
{
    [TestClass]
    public class ReaderLocalTableTest
    {
        private static readonly string LocalSymbolTablePrefix = SystemSymbols.IonSymbolTable + "::";

        [TestMethod]
        public void WithImport_FindCorrectSymbol()
        {
            void AssertTable(ISymbolTable tab, params (string sym, int id)[] syms)
            {
                foreach (var sym in syms)
                {
                    var symtok = tab.Find(sym.sym);
                    Assert.AreEqual(sym, sym, symtok.Text);
                    Assert.AreEqual(SymbolToken.UnknownSid, symtok.Sid);

                    var symText = tab.FindKnownSymbol(sym.id);
                    Assert.AreEqual(sym.sym, symText);

                    var sid = tab.FindSymbolId(sym.sym);
                    Assert.AreEqual(sym.id, sid);
                }
            }

            var table = new ReaderLocalTable(SharedSymbolTable.GetSystem(1));
            var shared = SharedSymbolTable.NewSharedSymbolTable("table", 1, null, new[] {"a", "b", "c"});

            table.Imports.Add(shared);
            table.Refresh();

            AssertTable(table,
                ("a", 10),
                ("b", 11),
                ("c", 12)
            );
        }

        [TestMethod]
        public void TestLocalSymbolTableAppend()
        {
            var text = LocalSymbolTablePrefix +
                    "{" +
                    "  symbols:[ \"s1\", \"s2\" ]" +
                    "}\n" +
                LocalSymbolTablePrefix +
                    "{" +
                    "  imports:" + SystemSymbols.IonSymbolTable + "," +
                    "  symbols:[ \"s3\", \"s4\", \"s5\" ]" +
                    "}\n" +
                    "null";

            ISymbolTable symbolTable = OneValue(text);
            CheckLocalTable(symbolTable);

            var systemMaxId = symbolTable.GetSystemTable().MaxId;

            // Table contains all symbols and SIDs are in correct order.
            CheckSymbol("s1", systemMaxId + 1, symbolTable);
            CheckSymbol("s2", systemMaxId + 2, symbolTable);
            CheckSymbol("s3", systemMaxId + 3, symbolTable);
            CheckSymbol("s4", systemMaxId + 4, symbolTable);
            CheckSymbol("s5", systemMaxId + 5, symbolTable);

            CheckUnknownSymbol("unknown", SymbolToken.UnknownSid, symbolTable);
            CheckUnknownSymbol(33, symbolTable);
        }

        [TestMethod]
        public void TestLocalSymbolTableAppendRoundTrip()
        {
            var text =
                LocalSymbolTablePrefix +
                "{" +
                "   imports:[{name:\"foo\", version:1, max_id:1}], " +
                "   symbols:[\"s1\", \"s2\"]" +
                "}\n" +
                "$10\n" + // Symbol with unknown text from "foo".
                "$11\n" + // s1.
                LocalSymbolTablePrefix +
                "{" +
                "   imports:" + SystemSymbols.IonSymbolTable + "," +
                "   symbols:[\"s3\"]" +
                "}\n" +
                "$12\n" + // s2.
                "$13"; // s3.

            var datagram = IonLoader.Default.Load(text);

            var fooSymbols = new List<string>() { "bar" };
            var fooTable = SharedSymbolTable.NewSharedSymbolTable("foo", 1, null, fooSymbols);

            var catalog = new SimpleCatalog();
            catalog.PutTable(fooTable);

            // Text.
            var textOutput = new StringWriter();
            var textWriter = IonTextWriterBuilder.Build(textOutput, Enumerable.Repeat(fooTable, 1));
            datagram.WriteTo(textWriter);
            textWriter.Finish();

            var textRoundTrip = IonLoader.WithReaderOptions(new ReaderOptions { Catalog = catalog }).Load(textOutput.ToString());

            Assert.AreEqual("bar", textRoundTrip.GetElementAt(0).StringValue);
            Assert.AreEqual("s1", textRoundTrip.GetElementAt(1).StringValue);
            Assert.AreEqual("s2", textRoundTrip.GetElementAt(2).StringValue);
            Assert.AreEqual("s3", textRoundTrip.GetElementAt(3).StringValue);

            // Binary.
            using (var binaryOutput = new MemoryStream())
            {
                var binaryWriter = IonBinaryWriterBuilder.Build(binaryOutput, Enumerable.Repeat(fooTable, 1));
                datagram.WriteTo(binaryWriter);
                binaryWriter.Finish();

                var binaryRoundTrip = IonLoader.WithReaderOptions(new ReaderOptions { Catalog = catalog, Format = ReaderFormat.Binary }).Load(binaryOutput.ToArray());

                Assert.AreEqual("bar", binaryRoundTrip.GetElementAt(0).StringValue);
                Assert.AreEqual("s1", binaryRoundTrip.GetElementAt(1).StringValue);
                Assert.AreEqual("s2", binaryRoundTrip.GetElementAt(2).StringValue);
                Assert.AreEqual("s3", binaryRoundTrip.GetElementAt(3).StringValue);
            }
        }

        [TestMethod]
        public void TestLocalSymbolTableMultiAppend()
        {
            var text =
                LocalSymbolTablePrefix +
                    "{" +
                    "  symbols:[ \"s1\", \"s2\" ]" +
                    "}\n" +
                LocalSymbolTablePrefix +
                    "{" +
                    "  imports:" + SystemSymbols.IonSymbolTable + "," +
                    "  symbols:[ \"s3\" ]" +
                    "}\n" +
                LocalSymbolTablePrefix +
                    "{" +
                    "  imports:" + SystemSymbols.IonSymbolTable + "," +
                    "  symbols:[ \"s4\", \"s5\" ]" +
                    "}\n" +
                LocalSymbolTablePrefix +
                    "{" +
                    "  imports:" + SystemSymbols.IonSymbolTable + "," +
                    "  symbols:[ \"s6\" ]" +
                    "}\n" +
                    "null";

            ISymbolTable symbolTable = OneValue(text);
            CheckLocalTable(symbolTable);

            var systemMaxId = symbolTable.GetSystemTable().MaxId;

            // Table contains all symbols and SIDs are in correct order.
            CheckSymbol("s1", systemMaxId + 1, symbolTable);
            CheckSymbol("s2", systemMaxId + 2, symbolTable);
            CheckSymbol("s3", systemMaxId + 3, symbolTable);
            CheckSymbol("s4", systemMaxId + 4, symbolTable);
            CheckSymbol("s5", systemMaxId + 5, symbolTable);
            CheckSymbol("s6", systemMaxId + 6, symbolTable);

            CheckUnknownSymbol("unknown", SymbolToken.UnknownSid, symbolTable);
            CheckUnknownSymbol(33, symbolTable);
        }

        [TestMethod]
        public void TestLocalSymbolTableAppendEmptyList()
        {
            var original =
                LocalSymbolTablePrefix +
                    "{" +
                    "  symbols:[ \"s1\" ]" +
                    "}\n";

            var appended = original +
                LocalSymbolTablePrefix +
                    "{" +
                    "  imports:" + SystemSymbols.IonSymbolTable + "," +
                    "  symbols:[]" +
                    "}\n";

            ISymbolTable originalSymbolTable = OneValue(original + "null");
            ISymbolTable appendedSymbolTable = OneValue(appended + "null") ;

            var originalSymbol = originalSymbolTable.Find("s1");
            var appendedSymbol = appendedSymbolTable.Find("s1");

            Assert.AreEqual(originalSymbol.Sid, appendedSymbol.Sid);
        }

        private static ISymbolTable OneValue(string text)
        {
            IIonReader reader = IonReaderBuilder.Build(text);
            reader.MoveNext();

            return reader.GetSymbolTable();
        }

        private static void CheckLocalTable(ISymbolTable symbolTable)
        {
            Assert.IsTrue(symbolTable.IsLocal);
            Assert.IsFalse(symbolTable.IsShared);
            Assert.IsFalse(symbolTable.IsSystem);
            Assert.IsFalse(symbolTable.IsSubstitute);
            Assert.IsNotNull(symbolTable.GetImportedTables());

            CheckUnknownSymbol("not defined", SymbolToken.UnknownSid, symbolTable);

            ISymbolTable systemTable = symbolTable.GetSystemTable();
            CheckSystemTable(systemTable);

            Assert.AreEqual(systemTable.IonVersionId, symbolTable.IonVersionId);
        }

        private static void CheckSystemTable(ISymbolTable symbolTable)
        {
            Assert.IsFalse(symbolTable.IsLocal);
            Assert.IsTrue(symbolTable.IsShared);
            Assert.IsTrue(symbolTable.IsSystem);
            Assert.IsFalse(symbolTable.IsSubstitute, "table is substitute");
            Assert.AreSame(symbolTable, symbolTable.GetSystemTable());
            Assert.AreEqual(SystemSymbols.Ion10MaxId, symbolTable.MaxId);
            Assert.AreEqual(SystemSymbols.Ion10, symbolTable.IonVersionId);
        }

        private static void CheckSymbol(string text, int sid, ISymbolTable symbolTable)
        {
            CheckSymbol(text, sid, false, symbolTable);
        }

        private static void CheckUnknownSymbol(string text, int sid, ISymbolTable symbolTable)
        {
            CheckUnknownSymbol(text, symbolTable);

            if (sid != SymbolToken.UnknownSid)
            {
                CheckUnknownSymbol(sid, symbolTable);
            }
        }

        private static void CheckUnknownSymbol(string text, ISymbolTable symbolTable)
        {
            Assert.AreEqual(default, symbolTable.Find(text));
            Assert.AreEqual(SymbolToken.UnknownSid, symbolTable.FindSymbolId(text));

            if (symbolTable.IsReadOnly)
            {
                try
                {
                    symbolTable.Intern(text);

                    Assert.Fail("Expected exception");
                }
                catch (NotSupportedException)
                {
                }
            }
        }

        private static void CheckSymbol(string text, int sid, bool dupe, ISymbolTable symbolTable)
        {
            Assert.IsTrue(!dupe || text != null);

            var msg = "text:" + text + " sid:" + sid;

            if (text != null)
            {
                if (sid != SymbolToken.UnknownSid)
                {
                    Assert.AreEqual(text, symbolTable.FindKnownSymbol(sid), msg);
                }
            }
            else 
            {
                // No text expected, must have SID.
                Assert.AreEqual(text /* null */, symbolTable.FindKnownSymbol(sid), msg);
            }
        }

        private static void CheckUnknownSymbol(int sid, ISymbolTable symbolTable)
        {
            CheckSymbol(null, sid, false, symbolTable);
        }
    }
}
