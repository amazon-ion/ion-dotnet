using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Internals
{
    [TestClass]
    public class SymbolTokenTest
    {
        [TestMethod]
        public void Init_SidAndTextUnknown()
        {
            var token = new SymbolToken();
            Assert.AreEqual(null, token.Text);
            Assert.AreEqual(SymbolToken.UnknownSid, token.Sid);
            Assert.AreEqual(token, SymbolToken.None);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(3)]
        [DataRow(8)]
        public void ArrayAllocation_AllSetToDefault(int arrayLength)
        {
            var array = new SymbolToken[arrayLength];
            foreach (var token in array)
            {
                Assert.AreEqual(default, token);
            }
        }

        [TestMethod]
        [DataRow(0, 1)]
        [DataRow(1, 2)]
        [DataRow(2, 4)]
        [DataRow(4, 8)]
        [DataRow(8, 16)]
        public void ArrayResize_RemainderSetToDefault(int oldLength, int newLength)
        {
            var sampleToken = CreateSampleToken();
            var array = new SymbolToken[oldLength];
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = CreateSampleToken();
            }

            Array.Resize(ref array, newLength);

            for (var i = 0; i < oldLength; i++)
            {
                Assert.AreEqual(sampleToken, array[i]);
            }

            for (var i = oldLength; i < newLength; i++)
            {
                Assert.AreEqual(SymbolToken.None, array[i]);
            }
        }

        private static SymbolToken CreateSampleToken() => new SymbolToken("yo", 30);
    }
}
