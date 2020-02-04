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

using System.Collections.Generic;
using IonDotnet.Internals;
using IonDotnet.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Utils
{
    [TestClass]
    public class SimpleCatalogTest
    {
        [TestMethod]
        public void OneTable_MissingVersion()
        {
            const string tName = "T";
            var cat = new SimpleCatalog();
            Assert.IsNull(cat.GetTable(tName));
            Assert.IsNull(cat.GetTable(tName, 3));

            var t1 = SharedSymbolTable.NewSharedSymbolTable(tName, 1, null, new[] {"yes", "no"});
            cat.PutTable(t1);
            Assert.AreSame(t1, cat.GetTable(tName));
            Assert.AreSame(t1, cat.GetTable(tName, 1));
            Assert.AreSame(t1, cat.GetTable(tName, 5));

            var t2 = SharedSymbolTable.NewSharedSymbolTable(tName, 2, null, new[] {"yes", "no", "maybe"});
            cat.PutTable(t2);
            Assert.AreSame(t1, cat.GetTable(tName, 1));
            Assert.AreSame(t2, cat.GetTable(tName, 2));
            Assert.AreSame(t2, cat.GetTable(tName, 5));
        }

        [TestMethod]
        [DataRow(1, 5, new[] {1})]
        [DataRow(2, 5, new[] {1, 2})]
        [DataRow(3, 5, new[] {2, 1, 3})]
        [DataRow(3, 5, new[] {3, 1, 2})]
        [DataRow(3, 5, new[] {3, 2, 1})]
        [DataRow(3, 5, new[] {2, 3, 1})]
        [DataRow(6, 5, new[] {6})]
        [DataRow(6, 5, new[] {6, 9})]
        [DataRow(6, 5, new[] {9, 6})]
        [DataRow(6, 5, new[] {9, 6, 4})]
        [DataRow(6, 5, new[] {3, 9, 6})]
        [DataRow(6, 5, new[] {3, 9, 6, 4})]
        [DataRow(6, 5, new[] {3, 9, 2, 6, 4})]
        public void GetTable_BestMatch(int expected, int requested, int[] available)
        {
            const string tName = "T";
            var map = new Dictionary<int, ISymbolTable>();
            var cat = new SimpleCatalog();
            foreach (var a in available)
            {
                map.Add(a, SharedSymbolTable.NewSharedSymbolTable(tName, a, null, new[] {"yes", "no"}));
                cat.PutTable(map[a]);
            }

            Assert.AreEqual(expected, cat.GetTable(tName, requested).Version);
        }
    }
}
