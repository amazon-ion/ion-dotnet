using System;
using System.Diagnostics;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using IonDotnet.Internals;

namespace IonDotnet.Bench
{
    public class Benchmarks
    {
        public byte[] _data;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _data = ReadDataFile("javaout");
        }

        [Benchmark]
        public void ReadStringNew()
        {
            using (var reader = new UserBinaryReader(new MemoryStream(_data)))
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
        }

        [Benchmark]
        public void Baseline()
        {
            using (var reader = new UserBinaryReader(new MemoryStream(_data)))
            {
                reader.Next();
            }
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
//            BenchmarkRunner.Run<Benchmarks>();

            var bm = new Benchmarks();
            bm.GlobalSetup();
            bm.ReadStringNew();
            var sw = new Stopwatch();
            sw.Start();
            sw.Stop();
            sw.Restart();
            sw.Stop();
            const int iter = 2000;
            long sum = 0;
            for (var i = 0; i < iter; i++)
            {
                sw.Restart();
                bm.ReadStringNew();
                sw.Stop();
                sum += sw.ElapsedTicks;
            }

            Console.WriteLine(sum * 1.0 / iter / 10);
        }
    }
}
