using System.IO;
using IonDotnet.Internals.Binary;
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
            var data = DirStructure.ReadDataFile("text/ts_2000_11_20_8_20_15_unknown.ion");
            IIonReader reader = new UserTextReader(new MemoryStream(data));
            ReaderTimestampTestCommon.Date_2000_11_20_8_20_15_Unknown(reader);
        }
    }
}
