using System.IO;
using IonDotnet.Internals.Text;
using IonDotnet.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Internals
{
    [TestClass]
    public class TextReaderTimestampTest
    {
        [TestMethod]
        public void Date_2000_11_20_8_20_15_Unknown()
        {
            var data = DirStructure.OwnTestFileAsBytes("text/ts_2000_11_20_8_20_15_unknown.ion");
            IIonReader reader = new UserTextReader(new MemoryStream(data));
            ReaderTimestampCommon.Date_2000_11_20_8_20_15_Unknown(reader);
        }

        /// <summary>
        /// Test the date string with >14 offset.
        /// </summary>
        /// <param name="dateString"></param>
        /// <param name="expectedDateString"></param>
        [DataRow("1857-05-30T19:24:59.1+23:59", "1857-05-29T19:25:59.1Z")]
        [TestMethod]
        public void Date_LargeOffset(string dateString, string expectedDateString)
        {
            var date = Timestamp.Parse(dateString);
            var expectedDate = Timestamp.Parse(expectedDateString);
            Assert.IsTrue(date.LocalOffset >= -14 * 60 && date.LocalOffset <= 14 * 60);
            Assert.IsTrue(date.Equals(expectedDate));
        }
    }
}
