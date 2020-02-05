using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.IonDotnet.Internals;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.IonDotnet.Tests.Internals
{
    [TestClass]
    public class BinaryWriterBufferTest
    {
        private class CustomSizeWriterBuffer : PagedWriterBuffer
        {
            public CustomSizeWriterBuffer(int intendedBlockSize) : base(intendedBlockSize)
            {
            }
        }

        [TestMethod]
        [DataRow("a")]
        [DataRow("abcdef352344324asdsaghijk")]
        [DataRow("😎💋🌹🎉🎂🤳💖🎶🤦‍♂️👌😒🤦‍♀️😉")]
        [DataRow(@" ط ظ ع غ ـ ف ق ك ل م ن ه و ى ي ً ٌ ٍ َ ُ ِ ّ ْ ٠ ١ ٢ ٣ ٤ ٥ ٦ ٧ ٨ ٩ ٪ ٫ ٬ ٭ ٰ ٱ ٲ ٳ ٴ ٵ ٶ ٷ ٸ ٹ ٺ ٻ ټ ٽ پ ٿ ڀ 
        ځ ڂ ڃ ڄ څ چ ڇ ڈ ډ ڊ ڋ ڌ ڍ ڎ ڏ ڐ ڑ ڒ ړ ڔ ڕ ږ ڗ ژ ڙ ښ ڛ ڜ ڝ ڞ ڟ ڠ ڡ ڢ ڣ ڤ ڥ ڦ ڧ ڨ ک ڪ ګ ڬ ڭ ڮ گ ")]
        [DataRow("Cũng phải có tý tiếng việt cho nó vui vẻ chứ nhỉ")]
        public void TestWriteShortString(string str)
        {
            IWriterBuffer buffer;
            using (buffer = new CustomSizeWriterBuffer(10))
            {
                var list = new List<Memory<byte>>();
                buffer.StartStreak(list);
                buffer.WriteUtf8(str.AsSpan());
                AssertByteSequence(str, buffer.Wrapup());
            }
        }

        private static void AssertByteSequence(string expectedString, IList<Memory<byte>> sequence)
        {
            var bc = Encoding.UTF8.GetByteCount(expectedString);
            var byteCount = sequence.Sum(s => s.Length);
            Assert.AreEqual(bc, byteCount);
            var bytes = new byte[byteCount];
            var offset = 0;
            foreach (var segment in sequence)
            {
                segment.CopyTo(new Memory<byte>(bytes, offset, segment.Length));
                offset += segment.Length;
            }

            var actual = Encoding.UTF8.GetString(bytes);
            Assert.AreEqual(expectedString, actual);
        }
    }
}
