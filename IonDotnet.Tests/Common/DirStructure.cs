using System;
using System.IO;
using System.Linq;

namespace IonDotnet.Tests.Common
{
    internal static class DirStructure
    {
        private static DirectoryInfo GetRootDir()
        {
            var dirInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (!string.Equals(dirInfo.Name, "iondotnet", StringComparison.OrdinalIgnoreCase))
            {
                dirInfo = Directory.GetParent(dirInfo.FullName);
            }

            return dirInfo;
        }

        private static DirectoryInfo TestDatDir()
        {
            var root = GetRootDir();
            return new DirectoryInfo(Path.Combine(
                root.FullName, "IonDotnet.Tests", "TestDat"));
        }

        // ion-tests/iontestdata/
        private static DirectoryInfo IonTestDir()
        {
            var root = GetRootDir();
            return new DirectoryInfo(Path.Combine(
                root.FullName, "ion-tests", "iontestdata"));
        }

        public static byte[] OwnTestFileAsBytes(string relativePath)
        {
            var testDatDir = TestDatDir();
            var path = Path.Combine(testDatDir.FullName, relativePath);
            return File.ReadAllBytes(path);
        }


        /// <remarks>Dispose this stream after using</remarks>
        public static Stream OwnTestFileAsStream(string relativePath)
        {
            var testDatDir = TestDatDir();
            var path = Path.Combine(testDatDir.FullName, relativePath);
            return new FileStream(path, FileMode.Open, FileAccess.Read);
        }

        public static byte[] IonTestFileAsBytes(string relativePath)
        {
            var testDatDir = IonTestDir();
            var path = Path.Combine(testDatDir.FullName, relativePath);
            return File.ReadAllBytes(path);
        }

        /// <remarks>Dispose this stream after using</remarks>
        public static Stream IonTestFileAsStream(string relativePath)
        {
            var testDatDir = IonTestDir();
            var path = Path.Combine(testDatDir.FullName, relativePath);
            return new FileStream(path, FileMode.Open, FileAccess.Read);
        }
    }
}
