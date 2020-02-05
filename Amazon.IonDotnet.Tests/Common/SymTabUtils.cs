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

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.IonDotnet.Tests.Common
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
            Assert.AreEqual(SymbolToken.UnknownSid, token.Sid);
            Assert.AreEqual(text, token.Text);

            token = symbolTable.Intern(text);
            Assert.AreEqual(SymbolToken.UnknownSid, token.Sid);
            Assert.AreEqual(text, token.Text);
        }
    }
}
