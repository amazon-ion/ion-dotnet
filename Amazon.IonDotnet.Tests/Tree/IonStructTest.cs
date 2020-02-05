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
using Amazon.IonDotnet.Tree.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.IonDotnet.Tests.Tree
{
    [TestClass]
    public class IonStructTest : IonContainerTest
    {
        private static int _serial = 1;

        protected override IIonValue MakeMutableValue()
        {
            return new IonStruct();
        }

        internal override IonContainer MakeNullValue()
        {
            return IonStruct.NewNull();
        }

        internal override void DoAdd(IonContainer container, IonValue item)
        {
            var fieldName = $"Field{_serial++}";
            var v = (IonStruct) container;
            v[fieldName] = item;
        }

        [TestMethod]
        public void Add_Replace()
        {
            const string field = "field";

            var v = (IonStruct) MakeMutableValue();
            Assert.AreEqual(0, v.Count);
            var c1 = (IonValue) MakeMutableValue();

            Assert.IsFalse(v.ContainsField(field));
            Assert.IsFalse(v.Contains(c1));
            v[field] = c1;
            Assert.AreEqual(1, v.Count);
            Assert.IsTrue(v.ContainsField(field));
            Assert.IsTrue(v.Contains(c1));
            Assert.AreEqual(c1, v[field]);

            var c2 = (IonValue) MakeMutableValue();
            v[field] = c2;
            Assert.AreEqual(1, v.Count);
            Assert.IsFalse(v.Contains(c1));
            Assert.IsTrue(v.Contains(c2));
            Assert.IsTrue(v.ContainsField(field));
            Assert.AreEqual(c2, v[field]);
        }

        [TestMethod]
        public void SameFieldName()
        {
            const string fieldName = "field";

            IonStruct create()
            {
                var ionStruct = (IonStruct) MakeMutableValue();
                ionStruct.Add(fieldName, new IonInt(1));
                ionStruct.Add(fieldName + "2", new IonBool(true));
                ionStruct.Add(fieldName, new IonInt(2));
                return ionStruct;
            }

            var v = create();

            //get first occurence
            var ionInt = (IonInt) v[fieldName];
            Assert.AreEqual(1, ionInt.IntValue);

            //check number of occurences
            var count = v.Count(val => val.FieldNameSymbol.Text == fieldName);
            Assert.AreEqual(2, count);

            //test remove fields
            v.RemoveField(fieldName);
            count = v.Count(val => val.FieldNameSymbol.Text == fieldName);
            Assert.AreEqual(0, count);
            Assert.AreEqual(1, v.Count);

            //test set field
            v = create();
            v[fieldName] = new IonBool(false);
            Assert.AreEqual(2, v.Count);
            Assert.AreEqual(1, v.Count(val => val.FieldNameSymbol.Text == fieldName));
            var ionBool = (IonBool) v[fieldName];
            Assert.IsFalse(ionBool.BoolValue);
        }

        [TestMethod]
        public void RemoveField()
        {
            const string field = "field";
            var v = (IonStruct) MakeMutableValue();
            var c = (IonValue) MakeMutableValue();
            v[field] = c;
            var removed = v.RemoveField(field);

            Assert.IsTrue(removed);
            Assert.AreEqual(0, v.Count);
            Assert.IsFalse(v.Contains(c));
            Assert.IsFalse(v.RemoveField(field));
        }

        [TestMethod]
        public void RemoveItem()
        {
            const string field = "field";
            var v = (IonStruct) MakeMutableValue();
            var c = (IonValue) MakeMutableValue();
            v[field] = c;
            v.Remove(c);
            Assert.AreEqual(0, v.Count);
            Assert.IsFalse(v.Contains(c));
        }

        [TestMethod]
        public void StructEquivalence_True()
        {
            var s1 = BuildFlatStruct(1, 10);
            var s2 = BuildFlatStruct(1, 10);
            Assert.IsTrue(s1.IsEquivalentTo(s2));

            //add fields with the same name
            s1.Add("new_field", new IonBool(true));
            s1.Add("new_field", new IonBool(true));
            s2.Add("new_field", new IonBool(true));
            s2.Add("new_field", new IonBool(true));
            Assert.IsTrue(s1.IsEquivalentTo(s2));

            var n1 = IonStruct.NewNull();
            var n2 = IonStruct.NewNull();
            Assert.IsTrue(n1.IsEquivalentTo(n2));
        }

        [TestMethod]
        public void StructEquivalence_False()
        {
            var s1 = BuildFlatStruct(1, 10);
            var s2 = BuildFlatStruct(1, 9);
            var n = IonStruct.NewNull();

            Assert.IsFalse(s1.IsEquivalentTo(s2));
            Assert.IsFalse(s1.IsEquivalentTo(n));
            Assert.IsFalse(n.IsEquivalentTo(s1));
            s2["field10"] = new IonBool(true);
            Assert.IsFalse(s1.IsEquivalentTo(s2));

            s2.RemoveField("field10");
            s2["field10"] = new IonInt(10);
            Assert.IsTrue(s1.IsEquivalentTo(s2));

            //different field name
            s2.RemoveField("field10");
            s2["another"] = new IonInt(10);
            Assert.IsFalse(s1.IsEquivalentTo(s2));
        }

        [TestMethod]
        public void StructEquivalence_False_SameFieldName()
        {
            var s1 = BuildFlatStruct(1, 10);
            var s2 = BuildFlatStruct(1, 10);
            s1.Add("new_field", new IonBool(true));
            s2.Add("new_field", new IonBool(false));
            Assert.IsFalse(s1.IsEquivalentTo(s2));
        }

        private IonStruct BuildFlatStruct(int start, int count)
        {
            var str = (IonStruct) MakeMutableValue();
            for (var i = 0; i < count; i++)
            {
                str[$"field{start + i}"] = new IonInt(start + i);
            }

            return str;
        }
    }
}
