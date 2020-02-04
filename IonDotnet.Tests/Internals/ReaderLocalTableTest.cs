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

using IonDotnet.Internals;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Internals
{
    [TestClass]
    public class ReaderLocalTableTest
    {
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
    }
}
