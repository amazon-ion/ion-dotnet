using System;
using System.Text;
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

        protected override IonValue MakeMutableValue()
        {
            return new IonClob(new byte[0]);
        }

        protected override IonLob MakeNullValue()
        {
            return IonClob.NewNull();
        }

        protected override IonLob MakeWithBytes(ReadOnlySpan<byte> bytes)
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
