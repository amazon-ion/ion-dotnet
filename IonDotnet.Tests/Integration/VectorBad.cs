using System;
using System.IO;
using System.Linq;
using IonDotnet.Systems;
using IonDotnet.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Integration
{
    [TestClass]
    public class VectorBad
    {
        private static readonly DirectoryInfo IonTestDir = DirStructure.IonTestDir();
        private static readonly DirectoryInfo BadDir = IonTestDir.GetDirectories("bad").First();

        private static FileInfo GetFile(DirectoryInfo dir, string name)
        {
            return new FileInfo(Path.Combine(dir.FullName, name));
        }

        [DataRow("annotationFalse.ion")]
        [DataRow("annotationNan.ion")]
        [DataRow("annotationNull.ion")]
        [DataRow("annotationNullInt.ion")]
        [DataRow("annotationSymbolIDUnmapped.ion")]
        [DataRow("annotationTrue.ion")]
        [DataRow("annotationWithoutValue.ion")]
        [TestMethod]
        [ExpectedException(typeof(IonException), AllowDerivedTypes = true)]
        public void Text_InvalidAnnotation(string fileName)
        {
            var fileInfo = GetFile(BadDir, fileName);
            IonLoader.WithReaderOptions(new ReaderOptions {Format = ReaderFormat.Text}).Load(fileInfo);
        }

        [DataRow("int_1.ion")]
        [DataRow("int_2.ion")]
        [DataRow("int_3.ion")]
        [DataRow("int_6.ion")]
        [DataRow("int_7.ion")]
        [DataRow("int_8.ion")]
        [DataRow("int_9.ion")]
        [DataRow("int_10.ion")]
        [TestMethod]
        [ExpectedException(typeof(FormatException), AllowDerivedTypes = true)]
        public void Int_Invalid(string fileName)
        {
            var fileInfo = GetFile(BadDir, fileName);
            IonLoader.WithReaderOptions(new ReaderOptions {Format = ReaderFormat.Text}).Load(fileInfo);
        }

        [DataRow("float_1.ion")]
        [DataRow("float_2.ion")]
        [DataRow("float_3.ion")]
        [DataRow("float_4.ion")]
        [DataRow("float_5.ion")]
        [DataRow("float_6.ion")]
        [DataRow("float_7.ion")]
        [DataRow("float_8.ion")]
        [DataRow("float_9.ion")]
        [DataRow("float_10.ion")]
        [DataRow("float_11.ion")]
        [TestMethod]
        [ExpectedException(typeof(FormatException), AllowDerivedTypes = true)]
        public void Float_Invalid(string fileName)
        {
            var fileInfo = GetFile(BadDir, fileName);
            IonLoader.WithReaderOptions(new ReaderOptions {Format = ReaderFormat.Text}).Load(fileInfo);
        }
        
        [DataRow("decimal_1.ion")]
        [DataRow("decimal_2.ion")]
        [DataRow("decimal_3.ion")]
        [DataRow("decimal_4.ion")]
        [DataRow("decimal_5.ion")]
        [DataRow("decimal_6.ion")]
        [DataRow("decimal_7.ion")]
        [DataRow("decimal_8.ion")]
        [DataRow("decimal_9.ion")]
        [DataRow("decimal_10.ion")]
        [DataRow("decimal_11.ion")]
        [DataRow("decimal_12.ion")]
        [DataRow("decimal_13.ion")]
        [DataRow("decimal_14.ion")]
        [TestMethod]
        [ExpectedException(typeof(FormatException), AllowDerivedTypes = true)]
        public void Decimal_Invalid(string fileName)
        {
            var fileInfo = GetFile(BadDir, fileName);
            IonLoader.WithReaderOptions(new ReaderOptions {Format = ReaderFormat.Text}).Load(fileInfo);
        }
    }
}
