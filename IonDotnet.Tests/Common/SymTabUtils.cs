using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Common
{
    internal static class SymTabUtils
    {
        internal static void AssertSymbolInTable(string text, int sid, bool duplicate, ISymbolTable symbolTable)
        {
            if (text == null)
            {
                Assert.IsNull(symbolTable.FindKnownSymbol(sid));
                return;
            }

            if (sid != SymbolToken.UnknownSid)
            {
                Assert.AreEqual(text, symbolTable.FindKnownSymbol(sid));
            }

            if (duplicate)
                return;

            Assert.AreEqual(sid, symbolTable.FindSymbolId(text));
            var token = symbolTable.Find(text);
            Assert.AreEqual(sid, token.Sid);
            Assert.AreEqual(text, token.Text);

            token = symbolTable.Intern(text);
            Assert.AreEqual(sid, token.Sid);
            Assert.AreEqual(text, token.Text);
        }
    }
}
