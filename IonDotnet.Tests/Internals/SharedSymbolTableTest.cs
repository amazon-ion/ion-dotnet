﻿/*
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
using System.Text;
using IonDotnet.Internals;
using IonDotnet.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Internals
{
    [TestClass]
    public class SharedSymbolTableTest
    {
        [TestMethod]
        [DataRow(new[] {"1", "2", "3"})]
        [DataRow(new[] {"1"})]
        [DataRow(new string[0])]
        public void BasicSymbol_NoImport_Valid(string[] symbols)
        {
            const string name = "tableName";
            const int version = 1;
            var table = SharedSymbolTable.NewSharedSymbolTable(name, version, null, symbols);
            AssertTable(table, name, version, symbols);
        }

        [TestMethod]
        [DataRow(null)]
        public void InternNull_ThrowsException(string nullString)
        {
            const string name = "tableName";
            const int version = 1;
            var table = SharedSymbolTable.NewSharedSymbolTable(name, version, null, new string[0]);
            Assert.ThrowsException<ArgumentNullException>(() => table.Intern(nullString));
        }

        [TestMethod]
        [DataRow("a")]
        [DataRow("abc")]
        public void InternKnownText_KeepEntry(string text)
        {
            var table = SharedSymbolTable.NewSharedSymbolTable("table", 1, null, new[] {text});
            var extraText = new StringBuilder(text).ToString();
            Assert.AreNotSame(text, extraText);

            var symtok = table.Find(extraText);

            //make sure that no extra allocation is made
            Assert.AreSame(text, symtok.Text);
            Assert.AreEqual(SymbolToken.UnknownSid, symtok.Sid);
        }

        [TestMethod]
        public void InternUnknownText_Throws()
        {
            var table = SharedSymbolTable.NewSharedSymbolTable("table", 1, null, new[] {"a", "b", "c"});
            Assert.ThrowsException<InvalidOperationException>(() => table.Intern("d"));
        }

        [TestMethod]
        [DataRow(new[] {"1", "2", "3"})]
        [DataRow(new[] {"1"})]
        [DataRow(new string[0])]
        public void FindToken(string[] symbols)
        {
            var table = SharedSymbolTable.NewSharedSymbolTable("table", 1, null, symbols);
            foreach (var symbol in symbols)
            {
                var token = table.Find(symbol);
                Assert.AreSame(symbol, token.Text);
            }

            var unknown = table.Find($"{string.Join(",", symbols)}key");
            Assert.AreEqual(SymbolToken.None, unknown);
            Assert.ThrowsException<ArgumentNullException>(() => table.Find(null));
        }

        private static void AssertTable(ISymbolTable table, string name, int version, IReadOnlyList<string> symbols)
        {
            Assert.IsTrue(table.IsShared);
            Assert.IsFalse(table.IsSystem);
            Assert.IsNull(table.GetSystemTable());
            Assert.IsTrue(table.IsReadOnly);
            Assert.AreEqual(name, table.Name);
            Assert.AreEqual(version, table.Version);
            Assert.AreEqual(0, table.GetImportedMaxId());
            Assert.AreEqual(symbols.Count, table.MaxId);

            var foundSymbols = new HashSet<string>();
            using (var iter = table.GetDeclaredSymbolNames().GetEnumerator())
            {
                for (var i = 0; i < symbols.Count; i++)
                {
                    var sid = i + 1;
                    var text = symbols[i];

                    Assert.IsTrue(iter.MoveNext());
                    Assert.AreEqual(text, iter.Current);

                    var duplicate = text != null && !foundSymbols.Add(text);
                    SymTabUtils.AssertSymbolInTable(text, sid, duplicate, table);
                }

                Assert.IsFalse(iter.MoveNext());
            }
        }
    }
}
