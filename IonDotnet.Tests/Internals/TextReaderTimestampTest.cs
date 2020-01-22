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

        /// <summary>
        /// Test local timezone offset
        /// </summary>
        /// <param name="dateString"></param>
        /// <param name="expectedLocalOffset"></param>
        [DataRow("2010-10-10T03:20+02:12", +(2 * 60 + 12))]
        [DataRow("2010-10-10T03:20-02:12", -(2 * 60 + 12))]
        [DataRow("2010-10-10T03:20+00:12", +(0 * 60 + 12))]
        [DataRow("2010-10-10T03:20+02:00", +(2 * 60 + 00))]
        [TestMethod]
        public void TimeZone_Hour_Minute(string dateString, int expectedLocalOffset)
        {
            var date = Timestamp.Parse(dateString);
            var localOffset = date.LocalOffset;
            var LocalDateTime = date.DateTimeValue.ToString();

            Assert.AreEqual(expectedLocalOffset, localOffset); 
            Assert.AreEqual("2010-10-10 3:20:00 AM", LocalDateTime);
        }
    }
}
