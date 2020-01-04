using System.Linq;
using IonDotnet.Builders;
using IonDotnet.Tests.Common;
using IonDotnet.Tree;
using IonDotnet.Tree.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Builders
{
    [TestClass]
    public class LoaderTest
    {
        /// <summary>
        /// See 'cascading_symtabs.ion'
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(UnknownSymbolException))]
        public void CascadingSymtab_TestCorrectSymbolDecoding()
        {
            var file = DirStructure.OwnFile("text/cascading_symtabs.ion");
            var datagram = IonLoader.Default.Load(file);

            Assert.AreEqual(3, datagram.Count);

            int counter = 0;
            foreach (var ionValue in datagram)
            {
                CascadingSymtabAssertion(ionValue, counter++);
            }
        }

        private void CascadingSymtabAssertion(IIonValue ionValue, int itemNumber)
        {
            Assert.AreEqual(IonType.Symbol, ionValue.Type());
            var token = ionValue.SymbolValue;
            switch (itemNumber)
            {
                case 0:
                    Assert.AreEqual(13, token.Sid);
                    Assert.IsNull(token.Text);
                    break;
                case 1:
                    Assert.AreEqual(10, token.Sid);
                    Assert.AreEqual("rock", token.Text);
                    break;
                case 2:
                    Assert.AreEqual(10, token.Sid);
                    Assert.IsNull(token.Text);
                    break;
            }
        }

        [TestMethod]
        public void TextLoader_SymbolAnnotation()
        {
            const string doc = "$3::123";
            var datagram = IonLoader.Default.Load(doc);
            Assert.AreEqual(1, datagram.Count);
            foreach (var ionValue in datagram)
            {
                var child = ionValue;
                Assert.AreEqual(0, datagram.IndexOf(child));
                var annots = child.GetTypeAnnotations();
                Assert.AreEqual(1, annots.Count);
                Assert.AreEqual(SystemSymbols.IonSymbolTable, annots.First().Text);
            }
        }

        [TestMethod]
        public void TextLoader_TripleQuotedClob()
        {
            const string doc = "{{'''hello'''}}";
            var datagram = IonLoader.Default.Load(doc);
            Assert.AreEqual(1, datagram.Count);
            foreach (var ionValue in datagram)
            {
                var child = ionValue;
                Assert.AreEqual(0, datagram.IndexOf(child));
                Assert.AreEqual("hello".Length, child.Bytes().Length);
            }
        }
    }
}
