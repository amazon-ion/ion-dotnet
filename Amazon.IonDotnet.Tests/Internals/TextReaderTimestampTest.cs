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
using System.IO;
using Amazon.IonDotnet.Internals.Text;
using Amazon.IonDotnet.Tests.Common;
using Amazon.IonDotnet.Builders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using System.Collections.Generic;

namespace Amazon.IonDotnet.Tests.Internals
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
        /// Verify that timestamp parsing is not affected by the runtime's configured locale.
        /// </summary>
        /// <param name="dateString"></param>
        [DataRow("2020-10-21T12:37:52.086Z"),
         DataRow("2020-10-21T12:37:52.186Z"),
         DataRow("2020-10-21T12:37:00.086Z"),
         DataRow("2020-10-21T13:18:46.911Z"),
         DataRow("2020-10-21T13:18:00.911Z")]
        [TestMethod]
        public void Date_IgnoreCultureInfo(string dateString)
        {
            // Take note of the system's default culture setting so we can restore it after this test is complete
            CultureInfo initialCulture = CultureInfo.CurrentCulture;

            // Parse the timestamp using the runtime's InvariantCulture. InvariantCulture is a stable, non-customizable locale 
            // that can be used for formatting and parsing operations that require culture-independent results.
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var invariantCultureTimestamp = Timestamp.Parse(dateString);

            // Parse the timestamp again for each of the following cultures, verifying that the result is the same as it was
            // when we parsed it using the InvariantCulture.
            List<string> cultureNames = new List<string>() {"af-ZA", "en-GB", "en-US", "es-CL", "es-MX", "es-US", "ko-KR", "nl-NL", "zh-CN"};

            cultureNames.ForEach(cultureName => {
                CultureInfo.CurrentCulture = new CultureInfo(cultureName, false);
                var variantCultureTimestamp = Timestamp.Parse(dateString);
                Assert.AreEqual(invariantCultureTimestamp, variantCultureTimestamp);
            });

            // Restore the original culture setting so the output of subsequent tests will be written using the expected
            // localization.
            CultureInfo.CurrentCulture = initialCulture;
        }

        /// <summary>
        /// Test local timezone offset
        /// </summary>
        /// <param name="dateString"></param>
        /// <param name="expectedLocalOffset"> Time zone offset in minutes</param>
        /// <param name="expectedTimeOffset"></param>
        [DataRow("2010-10-10T03:20+02:12", +(2 * 60 + 12), 3, 20)]
        [DataRow("2010-10-10T03:20-02:12", -(2 * 60 + 12), 3, 20)]
        [DataRow("2010-10-10T03:20+00:12", +(0 * 60 + 12), 3, 20)]
        [DataRow("2010-10-10T03:20+02:00", +(2 * 60 + 00), 3, 20)]
        [TestMethod]
        public void TimeZone_Hour_Minute(string dateString, int expectedOffset, int expectedHour, int expectedMinute)
        {
            var date = Timestamp.Parse(dateString);
            var dateTimeOffset = date.AsDateTimeOffset();

            Assert.AreEqual(expectedOffset, date.LocalOffset);
            Assert.AreEqual(expectedHour, dateTimeOffset.Hour);
            Assert.AreEqual(expectedMinute, dateTimeOffset.Minute);
        }
         
        [DataRow("2010-10-10T03:20")]
        [DataRow("2010-10-10T03:20:40")]
        [DataRow("2010-10-10T03:20:40.5")]
        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void Invalid_Timestamps_Missing_Offset(string dateString)
        {
            Timestamp.Parse(dateString);
        }
    }
}
