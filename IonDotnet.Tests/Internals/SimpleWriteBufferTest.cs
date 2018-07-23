using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IonDotnet.Internals;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Internals
{
    [TestClass]
    public class SimpleWriteBufferTest
    {
        [TestMethod]
        [DataRow("a")]
        [DataRow("abcdefddshgrhgldutrihfdjlbdjksfbaskhdgasygfadksfhlsdfjkldjasdjladhoafhydoshv")]
        [DataRow("😀 😁 😂 🤣 😃 😄 😅 😆 😉")]
        [DataRow("Viết thử tý tiếng việt xem nó có hoạt động không, mà chắc là chạy thôi ahihi")]
        [DataRow(" Բ Գ Դ Ե Զ Է Ը Թ Ժ Ի Լ Խ Ծ Կ Հ Ձ Ղ Ճ Մ Յ Ն Շ Ո Չ Պ Ջ Ռ Ս Վ Տ Ր Ց Ւ ")]
        [DataRow("㈍ ㈎ ㈏ ㈐ ㈑ ㈒ ㈓ ㈔ ㈕ ㈖ ㈗ ㈘ ㈙ ㈚ ㈛ ㈜ ㈠ ㈡ ㈢ ㈣ ㈤ ㈥ ㈦ ㈧ ㈨ ㈩ ㈪ ㈫ ㈬ ㈭ ㈮ ㈯ ㈰ ㈱ ㈲")]
        public void WriteString(string str)
        {
            IWriteBuffer writerBuffer;
            using (writerBuffer = new SimpleWriteBuffer())
            {
                var list = new List<Memory<byte>>();
                writerBuffer.StartStreak(list);
                writerBuffer.WriteUtf8(str);
                AssertString(str, writerBuffer.Wrapup());
            }
        }


        private static void AssertString(string expected, IList<Memory<byte>> bytes)
        {
            var byteCount = bytes.Sum(s => s.Length);

            Assert.AreEqual(Encoding.UTF8.GetByteCount(expected), byteCount);
            var byteArray = new byte[byteCount];
            var offset = 0;
            foreach (var memory in bytes)
            {
                memory.CopyTo(new Memory<byte>(byteArray, offset, memory.Length));
                offset += memory.Length;
            }

            var decodedString = Encoding.UTF8.GetString(byteArray);
            for (var i = 0; i < decodedString.Length; i++)
            {
                if (expected[i] != decodedString[i])
                {
                    Console.WriteLine(Convert.ToInt32(expected[i]));
                    Console.WriteLine(Convert.ToInt32(decodedString[i]));
                }

                Assert.AreEqual(expected[i], decodedString[i]);
            }
        }
    }
}
