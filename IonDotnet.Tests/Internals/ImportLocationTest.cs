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

namespace IonDotnet.Tests.Internals
{
    [TestClass]
    public class ImportLocationTest
    {
        [TestMethod]
        public void Init_SidAndTextUnknown()
        {
            var location = new ImportLocation();
            Assert.AreEqual(null, location.ImportName);
            Assert.AreEqual(0, location.Sid);
        }

        [TestMethod]
        [DataRow("text1", 123, "text1", 123)]
        [DataRow("text2", 456, "text2", 456)]
        public void Bool_EqualsOperator(string text1, int sid1, string text2, int sid2)
        {
            var location1 = new ImportLocation(text1, sid1);
            var location2 = new ImportLocation(text2, sid2);
            Assert.IsTrue(location1 == location2);
        }

        [TestMethod]
        [DataRow("text1", 123, "text1", 456)]
        [DataRow("text2", 456, "text3", 456)]
        public void Bool_NotEqualsOperator(string text1, int sid1, string text2, int sid2)
        {
            var location1 = new ImportLocation(text1, sid1);
            var location2 = new ImportLocation(text2, sid2);
            Assert.IsTrue(location1 != location2);
        }

        [TestMethod]
        public void EqualsMethod()
        {
            var location = CreateSampleToken();
            var equalLocation = new ImportLocation("yo", 30);
            var unEqualLocation = new ImportLocation("yo", 31);
            Assert.IsTrue(location.Equals(equalLocation));
            Assert.IsFalse(location.Equals(unEqualLocation));
        }

        private static ImportLocation CreateSampleToken() => new ImportLocation("yo", 30);
    }
}
