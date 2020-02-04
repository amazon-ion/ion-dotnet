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
        [DataRow("text1", 123, "text1", 123)]
        [DataRow("text2", 456, "text2", 456)]
        public void Bool_EqualsOperator(string text1, int sid1, string text2, int sid2)
        {
            var token1 = new SymbolToken(text1, sid1);
            var token2 = new SymbolToken(text2, sid2);
            Assert.IsTrue(token1 == token2);
        }

        [TestMethod]
        [DataRow("text1", 123, "text1", 456)]
        [DataRow("text2", 456, "text3", 456)]
        public void Bool_NotEqualsOperator(string text1, int sid1, string text2, int sid2)
        {
            var token1 = new SymbolToken(text1, sid1);
            var token2 = new SymbolToken(text2, sid2);
            Assert.IsTrue(token1 != token2);
        }

        [TestMethod]
        public void EqualsMethod()
        {
            var token = CreateSampleToken();
            var equalToken = new SymbolToken("yo", 30);
            var unEqualToken = new SymbolToken("yo", 31);
            Assert.IsTrue(token.Equals(equalToken));
            Assert.IsFalse(token.Equals(unEqualToken));
        }

        [TestMethod]
        public void IsEquivalentTo()
        {
            var token = CreateSampleTokenWithImportLocation();
            var equalToken = new SymbolToken("yo", 30, new ImportLocation("hey", 40));
            var unEqualToken = new SymbolToken("oy", 30, new ImportLocation("hey", 41));
            Assert.IsTrue(token.IsEquivalentTo(equalToken));
            Assert.IsFalse(token.IsEquivalentTo(unEqualToken));
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
        private static SymbolToken CreateSampleTokenWithImportLocation() => new SymbolToken("yo", 30, new ImportLocation("hey", 40));
    }
}
