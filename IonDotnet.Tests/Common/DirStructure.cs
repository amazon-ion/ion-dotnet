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
            while (!string.Equals(dirInfo.Name, "iondotnet.tests", StringComparison.OrdinalIgnoreCase))
            {
                dirInfo = Directory.GetParent(dirInfo.FullName);
            }

            return dirInfo;
        }

        private static DirectoryInfo TestDatDir()
        {
            var root = GetRootDir();
            return root.GetDirectories().Single(d => string.Equals(d.Name, "testdat", StringComparison.OrdinalIgnoreCase));
        }

        public static byte[] ReadDataFile(string relativePath)
        {
            var testDatDir = TestDatDir();
            var path = Path.Combine(testDatDir.FullName, relativePath);
            return File.ReadAllBytes(path);
        }
    }
}
