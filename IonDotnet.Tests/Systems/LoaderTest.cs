using IonDotnet.Systems;
using IonDotnet.Tests.Common;
using IonDotnet.Tree;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Systems
{
    [TestClass]
    public class LoaderTest
    {
        /// <summary>
        /// See 'cascading_symtabs.ion'
        /// </summary>
        [TestMethod]
        public void CascadingSymtab_TestCorrectSymbolDecoding()
        {
            var file = DirStructure.OwnFile("text/cascading_symtabs.ion");
            var datagram = IonLoader.Default.Load(file);

            Assert.AreEqual(3, datagram.Count);

            //the first value $13 should be unknown text
            Assert.AreEqual(IonType.Symbol, datagram[0].Type);
            var token = ((IonSymbol) datagram[0]).SymbolValue;
            Assert.AreEqual(13, token.Sid);
            Assert.IsNull(token.Text);

            //2nd value $10 should be rock:10
            Assert.AreEqual(IonType.Symbol, datagram[1].Type);
            token = ((IonSymbol) datagram[1]).SymbolValue;
            Assert.AreEqual(10, token.Sid);
            Assert.AreEqual("rock", token.Text);

            //3rd value $10 should be unknown text
            Assert.AreEqual(IonType.Symbol, datagram[0].Type);
            token = ((IonSymbol) datagram[0]).SymbolValue;
            Assert.AreEqual(10, token.Sid);
            Assert.IsNull(token.Text);
        }
    }
}
