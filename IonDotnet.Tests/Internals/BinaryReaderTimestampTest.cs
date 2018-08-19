using System.IO;
using IonDotnet.Internals.Binary;
using IonDotnet.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Internals
{
    [TestClass]
    public class BinaryReaderTimestampTest
    {
        [TestMethod]
        public void Date_2000_11_20_8_20_15_Unknown()
        {
            var data = DirStructure.ReadDataFile("binary/ts_2000_11_20_8_20_15_unknown.bindat");
            IIonReader reader = new UserBinaryReader(new MemoryStream(data));
            ReaderTimestampCommon.Date_2000_11_20_8_20_15_Unknown(reader);
        }
    }
}
