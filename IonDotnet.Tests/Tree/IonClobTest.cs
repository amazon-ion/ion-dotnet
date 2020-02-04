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
using System.Text;
using IonDotnet.Tree;
using IonDotnet.Tree.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Tree
{
    [TestClass]
    public class IonClobTest : IonLobTest
    {
        private static readonly Encoding[] Encodings =
        {
            Encoding.Unicode,
            Encoding.UTF8
        };

        protected override IIonValue MakeMutableValue()
        {
            return new IonClob(new byte[0]);
        }

        protected override IIonValue MakeNullValue()
        {
            return IonClob.NewNull();
        }

        protected override IIonValue MakeWithBytes(ReadOnlySpan<byte> bytes)
        {
            return new IonClob(bytes);
        }

        protected override IonType MainIonType => IonType.Clob;

        [DataRow("")]
        [DataRow("some english text")]
        [DataRow("∮ E⋅da = Q,  n → ∞, ∑ f(i) = ∏ g(i), ∀x∈ℝ: ⌈x⌉ = −⌊−x⌋, α ∧ ¬β = ¬(¬α ∨ β),")]
        [DataRow("â ă ô ớ")]
        [DataRow("ახლავე გაიაროთ რეგისტრაცია Unicode-ის მეათე საერთაშორისო")]
        [TestMethod]
        public void NewStreamReader(string text)
        {
            foreach (var encoding in Encodings)
            {
                var bytes = encoding.GetBytes(text);
                var v = new IonClob(bytes);
                using (var reader = v.NewReader(encoding))
                {
                    var t = reader.ReadToEnd();
                    Assert.AreEqual(text, t);
                }
            }
        }
    }
}
