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
