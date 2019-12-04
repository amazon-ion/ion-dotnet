using System;
using System.IO;
using IonDotnet.Internals.Binary;
using IonDotnet.Systems;
using IonDotnet.Tests.Common;
using IonDotnet.Tree.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Internals
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

        [DataRow("2008-12-23T23:00:01.123+07:00")]
        [TestMethod]
        public void Date_FragSecond(string dateStr)
        {
            var ts = Timestamp.Parse(dateStr);
            using (var memStream = new MemoryStream())
            {
                var binWriter = IonBinaryWriterBuilder.Build(memStream);
                binWriter.WriteTimestamp(ts);
                binWriter.Finish();
                var bytes = memStream.ToArray();
                var dg = IonLoader.Default.Load(bytes);
                Assert.IsTrue(dg[0] is IonTimestamp);
                var ionTimestamp = (IonTimestamp) dg[0];
                Assert.AreEqual(0.123 * TimeSpan.TicksPerSecond, ionTimestamp.Value.DateTimeValue.Ticks % TimeSpan.TicksPerSecond);
            }
        }
    }
}
