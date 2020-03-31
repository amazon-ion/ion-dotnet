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
using Amazon.IonDotnet.Builders;
using Amazon.IonDotnet.Internals;
using Amazon.IonDotnet.Tree;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.IonDotnet.Tests.Internals
{
    [TestClass]
    public class ReaderLocalTableTest
    {
        internal const int UNKNOWN_SYMBOL_ID = -1;
        internal static string ION_SYMBOL_TABLE = "$ion_symbol_table";
        internal static string LocalSymbolTablePrefix = ION_SYMBOL_TABLE + "::";
        internal static string ION_1_0 = "$ion_1_0";

        [TestMethod]
        public void WithImport_FindCorrectSymbol()
        {
            void assertTable(ISymbolTable tab, params (string sym, int id)[] syms)
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
            assertTable(table,
                ("a", 10),
                ("b", 11),
                ("c", 12)
            );
        }

        [TestMethod]
        public void TestLocalSymbolTableAppend()
        {
            string text = LocalSymbolTablePrefix +
                    "{" +
                    "  symbols:[ \"s1\", \"s2\"]" +
                    "}\n" +
                LocalSymbolTablePrefix +
                    "{" +
                    "  imports:" + ION_SYMBOL_TABLE + "," +
                    "  symbols:[ \"s3\", \"s4\", \"s5\"]" +
                    "}\n" +
                    "null";

            ISymbolTable symbolTable = OneValue(text);
            CheckLocalTable(symbolTable);
            int systemMaxId = symbolTable.GetSystemTable().MaxId;

            // table contains all symbols and SIDs are in correct order
            CheckSymbol("s1", systemMaxId + 1, symbolTable);
            CheckSymbol("s2", systemMaxId + 2, symbolTable);
            CheckSymbol("s3", systemMaxId + 3, symbolTable);
            CheckSymbol("s4", systemMaxId + 4, symbolTable);
            CheckSymbol("s5", systemMaxId + 5, symbolTable);

            CheckUnknownSymbol("unknown", UNKNOWN_SYMBOL_ID, symbolTable);
            CheckUnknownSymbol(33, symbolTable);
        }

        [TestMethod]
        public void TestLocalSymbolTableMultiAppend()
        {
            string text =
                LocalSymbolTablePrefix +
                    "{" +
                    "  symbols:[ \"s1\", \"s2\"]" +
                    "}\n" +
                LocalSymbolTablePrefix +
                    "{" +
                    "  imports:" + ION_SYMBOL_TABLE + "," +
                    "  symbols:[ \"s3\"]" +
                    "}\n" +
                LocalSymbolTablePrefix +
                    "{" +
                    "  imports:" + ION_SYMBOL_TABLE + "," +
                    "  symbols:[\"s4\", \"s5\"]" +
                    "}\n" +
                LocalSymbolTablePrefix +
                    "{" +
                    "  imports:" + ION_SYMBOL_TABLE + "," +
                    "  symbols:[\"s6\"]" +
                    "}\n" +
                    "null";

            ISymbolTable symbolTable = OneValue(text);
            CheckLocalTable(symbolTable);
            int systemMaxId = symbolTable.GetSystemTable().MaxId;

            // table contains all symbols and SIDs are in correct order
            CheckSymbol("s1", systemMaxId + 1, symbolTable);
            CheckSymbol("s2", systemMaxId + 2, symbolTable);
            CheckSymbol("s3", systemMaxId + 3, symbolTable);
            CheckSymbol("s4", systemMaxId + 4, symbolTable);
            CheckSymbol("s5", systemMaxId + 5, symbolTable);
            CheckSymbol("s6", systemMaxId + 6, symbolTable);

            CheckUnknownSymbol("unknown", UNKNOWN_SYMBOL_ID, symbolTable);
            CheckUnknownSymbol(33, symbolTable);
        }

        [TestMethod]
        public void TestLocalSymbolTableAppendEmptyList()
        {
            string original =
                LocalSymbolTablePrefix +
                    "{" +
                    "  symbols:[ \"s1\"]" +
                    "}\n";

            string appended = original +
                LocalSymbolTablePrefix +
                    "{" +
                    "  imports:" + ION_SYMBOL_TABLE + "," +
                    "  symbols:[]" +
                    "}\n";

            ISymbolTable originalSymbolTable = OneValue(original + "null");
            ISymbolTable appendedSymbolTable = OneValue(appended + "null") ;

            SymbolToken originalSymbol = originalSymbolTable.Find("s1");
            SymbolToken appendedSymbol = appendedSymbolTable.Find("s1");

            Assert.AreEqual(originalSymbol.Sid, appendedSymbol.Sid);
        }

        [TestMethod]
        public void TestLocalSymbolTableAppendImportBoundary()
        {
            string text =
                LocalSymbolTablePrefix +
                    "{" +
                    "  symbols:[ \"s11\"]" +
                    "}\n" +
                    "1\n";

            string text2 = 
                LocalSymbolTablePrefix +
                    "{" +
                    "  imports:" + ION_SYMBOL_TABLE + "," +
                    "  symbols:[ \"s21\"]" +
                    "}\n" +
                    "null";

            ISymbolTable original = OneValue(text);
            ISymbolTable appended = OneValue(text2);

            //TODO this is not implemented, the test could be failing because of this
            //original.Intern("o1");
            //appended.Intern("a1");
            int systemMaxId = appended.GetSystemTable().MaxId;

            // new symbols in `original` don't influence SIDs for new symbols in `appended` after import
            CheckSymbol("s11", systemMaxId + 1, appended);
            CheckSymbol("o1", systemMaxId + 2, original);

            CheckSymbol("s11", systemMaxId + 1, appended);
            CheckSymbol("s21", systemMaxId + 2, appended);
            CheckSymbol("a1", systemMaxId + 3, appended);

            // new symbols in `original` are not accessible from `appended` after import
            Assert.IsNull(original.Find("a1"));
            Assert.IsNull(original.Find("o1"));
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

            CheckUnknownSymbol(" not defined ", UNKNOWN_SYMBOL_ID, symbolTable);

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
            Assert.AreEqual(ION_1_0, symbolTable.IonVersionId);
        }

        private static void CheckSymbol(string text, int sid, ISymbolTable symbolTable)
        {
            CheckSymbol(text, sid, false, symbolTable);
        }

        private static void CheckUnknownSymbol(string text, int sid, ISymbolTable symbolTable)
        {
            CheckUnknownSymbol(text, symbolTable);

            if (sid != UNKNOWN_SYMBOL_ID)
            {
                CheckUnknownSymbol(sid, symbolTable);
            }
        }

        private static void CheckUnknownSymbol(string text, ISymbolTable symbolTable)
        {
            Assert.AreEqual(default, symbolTable.Find(text));
            Assert.AreEqual(UNKNOWN_SYMBOL_ID, symbolTable.FindSymbolId(text));
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

            string msg = "text:" + text + " sid:" + sid;

            if (text != null)
            {
                if (sid != UNKNOWN_SYMBOL_ID)
                {
                    Assert.AreEqual(text, symbolTable.FindKnownSymbol(sid), msg);
                }

                //Can't do this stuff when we have duplicate symbol.
                if (!dupe)
                {
                    Assert.AreEqual(sid, symbolTable.FindSymbolId(text), msg);

                    //TODO - currently commented out because it is not implemented
                    //SymbolToken symbolToken = symbolTable.Intern(text);
                    //Assert.AreEqual(sid, symbolToken.Sid, msg);

                    //symbolToken = symbolTable.Find(text);
                    //Assert.AreEqual(sid, symbolToken.Sid, msg);
                }
            }
            else //No text expected, must have sid
            {
                Assert.AreEqual(text /* null */, symbolTable.FindKnownSymbol(sid), msg);
            }
        }

        private static void CheckUnknownSymbol(int sid, ISymbolTable symbolTable)
        {
            CheckSymbol(null, sid, false, symbolTable);
        }
    }
}
