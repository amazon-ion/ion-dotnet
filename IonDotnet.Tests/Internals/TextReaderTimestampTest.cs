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
        /// <param name="expectedLocalOffset"> Time zone offset in minutes</param>
        /// <param name="expectedTimeOffset"></param>
        [DataRow("2010-10-10T03:20+02:12", +(2 * 60 + 12), "3:20:00 AM +02:12")]
        [DataRow("2010-10-10T03:20-02:12", -(2 * 60 + 12), "3:20:00 AM -02:12")]
        [DataRow("2010-10-10T03:20+00:12", +(0 * 60 + 12), "3:20:00 AM +00:12")]
        [DataRow("2010-10-10T03:20+02:00", +(2 * 60 + 00), "3:20:00 AM +02:00")]
        [TestMethod]
        public void TimeZone_Hour_Minute(string dateString, int expectedLocalOffset, string expectedTimeOffset)
        {
            var date = Timestamp.Parse(dateString);
            var localOffset = date.LocalOffset;
            var LocalDateTime = ExtractTimeAndTimeZone(date.AsDateTimeOffset());

            Assert.AreEqual(expectedLocalOffset, localOffset); 
            Assert.AreEqual(expectedTimeOffset, LocalDateTime);
        }

        private string ExtractTimeAndTimeZone(System.DateTimeOffset localDateTime)
        {
            var value = localDateTime.ToString();
            return value.Substring(value.Length - 17);
        }
    }
}
