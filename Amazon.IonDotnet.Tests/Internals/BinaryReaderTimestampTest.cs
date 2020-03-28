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
using Amazon.IonDotnet.Builders;
using Amazon.IonDotnet.Internals.Binary;
using Amazon.IonDotnet.Tests.Common;
using Amazon.IonDotnet.Tree.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.IonDotnet.Tests.Internals
{
    [TestClass]
    public class BinaryReaderTimestampTest
    {
        [TestMethod]
        public void Date_2000_11_20_8_20_15_Unknown()
        {
            var data = DirStructure.OwnTestFileAsBytes("binary/ts_2000_11_20_8_20_15_unknown.bindat");
            IIonReader reader = new UserBinaryReader(new MemoryStream(data));
            ReaderTimestampCommon.Date_2000_11_20_8_20_15_Unknown(reader);
        }

        [DataRow("2008T", DateTimeKind.Unspecified, 0, "2008T")]
        [DataRow("2008-02T", DateTimeKind.Unspecified, 0, "2008-02T")]
        [DataRow("2008-02-03T", DateTimeKind.Unspecified, 0, "2008-02-03T")]
        [DataRow("2008-12-23T23:22:00-00:00", DateTimeKind.Unspecified, 0, "2008-12-23T23:22:00")]
        [DataRow("2008-12-23T23:22:33", DateTimeKind.Unspecified, 0, "2008-12-23T23:22:33")]
        [DataRow("2008-12-23T23:22+00:00", DateTimeKind.Utc, 0, "2008-12-23T23:22Z")]
        [DataRow("2008-12-23T23:22+07:20", DateTimeKind.Local, 440, "2008-12-23T16:02Z")]
        [DataRow("2008-12-23T23:22-07:20", DateTimeKind.Local, -440, "2008-12-24T06:42Z")]
        [DataRow("2008-12-23T23:22:33+07:20", DateTimeKind.Local, 440, "2008-12-23T16:02:33Z")]
        [DataRow("2008-12-23T23:00:01.123+07:00", DateTimeKind.Local, 420, "2008-12-23T16:00:01.123Z")]
        [TestMethod]
        public void Date_FracSecond_offset(string dateStr, DateTimeKind expectedOffsetKind,
            int expectedOffsetValue, string expectedStr)
        {
            var timestamp = Timestamp.Parse(dateStr);
            var expected = Timestamp.Parse(expectedStr);
            using (var memStream = new MemoryStream())
            {
                var binWriter = IonBinaryWriterBuilder.Build(memStream);
                binWriter.WriteTimestamp(timestamp);
                binWriter.Finish();
                var bytes = memStream.ToArray();
                var datagram = IonLoader.Default.Load(bytes);
                foreach (var ionValue in datagram)
                {
                    Assert.IsTrue(ionValue is IonTimestamp);
                    var ionTimestamp = ionValue.TimestampValue;
                    Assert.AreEqual(expected.FractionalSecond, ionTimestamp.FractionalSecond);
                    Assert.AreEqual(expectedOffsetValue, ionTimestamp.LocalOffset);
                    Assert.AreEqual(expected.TimestampPrecision, ionTimestamp.TimestampPrecision);
                    Assert.AreEqual(expectedOffsetKind, ionTimestamp.DateTimeValue.Kind);
                    Assert.IsTrue(expected.Equals(ionTimestamp));

                }
            }
        }
    }
}
