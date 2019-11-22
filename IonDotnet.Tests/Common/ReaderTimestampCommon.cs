using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Common
{
    internal static class ReaderTimestampCommon
    {
        public static void Date_2000_11_20_8_20_15_Unknown(IIonReader reader)
        {
            Assert.AreEqual(IonType.Timestamp, reader.MoveNext());
            var timeStamp = reader.TimestampValue();
            var datetime = new DateTime(2000, 11, 20, 8, 20, 15, DateTimeKind.Unspecified);
            Assert.AreEqual(datetime, timeStamp.DateTimeValue);
            Assert.IsFalse(timeStamp.OffsetKnown);
        }
    }
}
