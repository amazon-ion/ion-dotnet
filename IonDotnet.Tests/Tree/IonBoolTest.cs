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
using IonDotnet.Tree;
using IonDotnet.Tree.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Tree
{
    [TestClass]
    public class IonBoolTest : TreeTestBase
    {
        protected override IIonValue MakeMutableValue() => new IonBool(false);

        [TestMethod]
        public void Null()
        {
            var n = IonBool.NewNull();
            Assert.AreEqual(IonType.Bool, n.Type());
            Assert.IsTrue(n.IsNull);
            Assert.ThrowsException<NullValueException>(() => n.BoolValue);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SimpleValue(bool value)
        {
            var v = new IonBool(value);
            Assert.AreEqual(IonType.Bool, v.Type());
            Assert.IsFalse(v.IsNull);
            Assert.AreEqual(value, v.BoolValue);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataRow(null)]
        [TestMethod]
        public void SetReadOnly(bool? value)
        {
            var v = value is null ? IonBool.NewNull() : new IonBool(value.Value);
            Assert.IsFalse(v.IsReadOnly);
            v.MakeReadOnly();
            Assert.IsTrue(v.IsReadOnly);
            Assert.ThrowsException<InvalidOperationException>(() => v.AddTypeAnnotation("something"));
            Assert.ThrowsException<InvalidOperationException>(() => v.MakeNull());
        }


        [DataRow(true)]
        [DataRow(false)]
        [TestMethod]
        public void BooleanEquality(bool value)
        {
            var nullBool = IonBool.NewNull();
            var null2 = IonBool.NewNull();
            var v = new IonBool(value);
            var v2 = new IonBool(value);
            var ionInt = new IonInt(3);
            var vd = new IonBool(!value);

            Assert.IsFalse(v.IsEquivalentTo(nullBool));
            Assert.IsFalse(v.IsEquivalentTo(vd));
            Assert.IsFalse(nullBool.IsEquivalentTo(v));
            Assert.IsTrue(nullBool.IsEquivalentTo(IonBool.NewNull()));
            Assert.IsTrue(v.IsEquivalentTo(v2));
            Assert.IsFalse(v.IsEquivalentTo(ionInt));
            Assert.IsTrue(nullBool.IsEquivalentTo(null2));
        }
    }
}
