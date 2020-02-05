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
using Amazon.IonDotnet.Tree;
using Amazon.IonDotnet.Tree.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.IonDotnet.Tests.Tree
{
    [TestClass]
    public class IonFloatTest : TreeTestBase
    {
        protected override IIonValue MakeMutableValue()
        {
            return new IonFloat(0.2);
        }

        [TestMethod]
        public void Null()
        {
            var n = IonFloat.NewNull();
            Assert.AreEqual(IonType.Float, n.Type());
            Assert.IsTrue(n.IsNull);
            Assert.ThrowsException<NullValueException>(() => n.DoubleValue);
        }

        [DataRow(0.0)]
        [DataRow(1.2)]
        [DataRow(0.012345678901234)]
        [TestMethod]
        public void SimpleValueTest(double value)
        {
            var v = new IonFloat(value);
            Assert.AreEqual(IonType.Float, v.Type());
            Assert.IsFalse(v.IsNull);
            Assert.AreEqual(value, v.DoubleValue);
        }

        [DataRow(null)]
        [DataRow(0.0)]
        [DataRow(1.2)]
        [DataRow(0.012345678901234)]
        [TestMethod]
        public void SetReadOnly(double? value)
        {
            var v = value == null ? IonFloat.NewNull() : new IonFloat(value.Value);
            Assert.IsFalse(v.IsReadOnly);
            v.MakeReadOnly();
            Assert.IsTrue(v.IsReadOnly);
            Assert.ThrowsException<InvalidOperationException>(() => v.AddTypeAnnotation("something"));
            Assert.ThrowsException<InvalidOperationException>(() => v.MakeNull());
        }

        [TestMethod]
        [DataRow(0.0)]
        [DataRow(1.2)]
        [DataRow(0.012345678901234)]
        public void FloatEquality(double value)
        {
            var v = new IonFloat(value);
            var vd = new IonFloat(value + 10e-14);
            var v2 = new IonFloat(value);
            var n = IonFloat.NewNull();
            var n2 = IonFloat.NewNull();
            var intVal = new IonInt(3);

            Assert.IsFalse(v.IsEquivalentTo(n));
            Assert.IsFalse(n.IsEquivalentTo(v));
            Assert.IsTrue(v.IsEquivalentTo(v2));
            Assert.IsFalse(v.IsEquivalentTo(vd));
            Assert.IsFalse(v.IsEquivalentTo(intVal));
            Assert.IsTrue(n.IsEquivalentTo(n2));
        }
    }
}
