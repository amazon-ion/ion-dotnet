using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using IonDotnet.Internals;

namespace IonDotnet.Bench
{
    [MemoryDiagnoser]
    public class Benchmarks
    {
        public byte[] _data;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _data = ReadDataFile("javaout");
        }

        [Benchmark]
        public void ReadStringOld()
        {
            using (var reader = new UserBinaryReader(new MemoryStream(_data), null, false))
            {
                Check(reader);
            }
        }

        [Benchmark]
        public void ReadStringNew()
        {
            using (var reader = new UserBinaryReader(new MemoryStream(_data)))
            {
                Check(reader);
            }
        }

        private static void Check(IIonReader reader)
        {
            reader.Next();
            reader.StepIn();

            while (reader.Next() != IonType.None)
            {
                //load the value
                reader.StringValue();
            }

            reader.StepOut();
        }

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
    }

    internal static class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<Benchmarks>();
        }
    }
}
