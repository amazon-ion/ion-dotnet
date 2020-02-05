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

using System.Linq;
using Amazon.IonDotnet.Tree;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.IonDotnet.Tests.Tree
{
    public abstract class TreeTestBase
    {
        protected abstract IIonValue MakeMutableValue();

        [DataRow(new string[0])]
        [DataRow(new[] {"a"})]
        [DataRow(new[] {"a", "b"})]
        [DataRow(new[] {"bool", "int"})]
        [TestMethod]
        public void AddAnnotations(string[] annotations)
        {
            var v = MakeMutableValue();
            Assert.AreEqual(0, v.GetTypeAnnotations().Count);

            foreach (var annotation in annotations)
            {
                v.AddTypeAnnotation(annotation);
            }

            Assert.AreEqual(annotations.Length, v.GetTypeAnnotations().Count);

            var annotReturns = v.GetTypeAnnotations();
            foreach (var annotation in annotations)
            {
                Assert.IsTrue(v.HasAnnotation(annotation));
                Assert.IsTrue(annotReturns.Any(a => a.Text == annotation));
            }
        }
    }
}
