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
using System.Collections.Generic;
using System.Diagnostics;
using IonDotnet.Tree.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Tree
{
    public abstract class IonContainerTest : TreeTestBase
    {
        /// <summary>
        /// Implementations should return a new null container.
        /// </summary>
        internal abstract IonContainer MakeNullValue();

        /// <summary>
        /// Implementations should add <paramref name="item"/> to <paramref name="container"/>.
        /// </summary>
        internal abstract void DoAdd(IonContainer container, IonValue item);

        [TestMethod]
        public void Null()
        {
            var v = (IonValue) MakeMutableValue();
            var n = MakeNullValue();
            Assert.AreEqual(v.Type(), n.Type());
            Assert.IsTrue(n.IsNull);
            Assert.AreEqual(0, n.Count);
            Assert.IsFalse(v.IsNull);
            Assert.ThrowsException<NullValueException>(() => DoAdd(n, v));
            if (n is IonSequence s)
            {
                Assert.ThrowsException<NullValueException>(() => s.Remove(v));
            }

            if (n is IonStruct ionStruct)
            {
                Assert.ThrowsException<NullValueException>(() => ionStruct.Remove(v));
            }
        }

        [TestMethod]
        public void SetReadOnly()
        {
            var v = (IonContainer) MakeMutableValue();
            Assert.IsFalse(v.IsReadOnly);
            v.MakeReadOnly();
            Assert.IsTrue(v.IsReadOnly);
            Assert.ThrowsException<InvalidOperationException>(() => DoAdd(v, MakeNullValue()));
            Assert.ThrowsException<InvalidOperationException>(() => v.Remove(MakeNullValue()));
        }

        [DataRow(0)]
        [DataRow(1)]
        [DataRow(10)]
        [DataRow(100)]
        [TestMethod]
        public void AddChildren_ClearChildren(int count)
        {
            var v = (IonContainer) MakeMutableValue();
            var list = new List<IonValue>();
            Assert.AreEqual(0, v.Count);
            for (var i = 0; i < count; i++)
            {
                var c = (IonValue) MakeMutableValue();
                list.Add(c);
                DoAdd(v, c);
            }

            Assert.AreEqual(count, v.Count);
            foreach (var c in v)
            {
                Assert.IsTrue(v.Contains(c));
            }

            //clear
            v.Clear();
            foreach (var c in list)
            {
                Assert.IsFalse(v.Contains(c));
            }
        }

        [DataRow(0)]
        [DataRow(1)]
        [DataRow(10)]
        [DataRow(100)]
        [TestMethod]
        public void MakeNull_ClearChildren(int count)
        {
            var v = (IonContainer) MakeMutableValue();
            var list = new List<IonValue>();
            Assert.AreEqual(0, v.Count);
            for (var i = 0; i < count; i++)
            {
                var c = (IonValue) MakeMutableValue();
                list.Add(c);
                DoAdd(v, c);
            }

            v.MakeNull();
            Assert.IsTrue(v.IsNull);
            foreach (var c in list)
            {
                Assert.IsFalse(v.Contains(c));
            }
        }

        [ExpectedException(typeof(NotSupportedException))]
        [TestMethod]
        public void Copy_NotSupported()
        {
            var v = (IonContainer) MakeMutableValue();
            DoAdd(v, (IonValue) MakeMutableValue());
            var arr = new IonValue[1];
            v.CopyTo(arr, 0);
        }

        [TestMethod]
        [DataRow(0, 1)]
        [DataRow(1, 5)]
        [DataRow(6, 10)]
        [DataRow(99, 100)]
        public void Remove(int idx, int count)
        {
            Debug.Assert(count > idx);
            var v = (IonContainer) MakeMutableValue();
            IonValue r = null;
            for (var i = 0; i < count; i++)
            {
                var c = (IonValue) MakeMutableValue();
                if (i == idx)
                {
                    r = c;
                }

                DoAdd(v, c);
            }

            Assert.IsTrue(v.Contains(r));
            v.Remove(r);
            Assert.AreEqual(count - 1, v.Count);
        }
    }
}
