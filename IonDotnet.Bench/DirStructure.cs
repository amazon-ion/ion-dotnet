using System;
using System.IO;

namespace IonDotnet.Bench
{
    public static class DirStructure
    {
        public static DirectoryInfo GetRootDir()
        {
            var dirInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (!string.Equals(dirInfo.Name, "iondotnet.bench", StringComparison.OrdinalIgnoreCase))
            {
                dirInfo = Directory.GetParent(dirInfo.FullName);
            }

            return dirInfo;
        }

        public static byte[] ReadDataFile(string relativePath)
        {
            var testDatDir = GetRootDir();
            var path = Path.Combine(testDatDir.FullName, relativePath);
            return File.ReadAllBytes(path);
        }

        public static FileStream OpenWrite(string fileName)
        {
            var rootDir = GetRootDir();
            return File.OpenWrite(Path.Combine(rootDir.FullName, fileName));
        }
    }
}
