using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using IonDotnet.Internals;

namespace IonDotnet.Bench
{
    [MemoryDiagnoser]
    public class Benchmarks
    {
//        public List<string> _data;

        public const string Sample = "SampleStringSampleStringSampleStringSampleStringSampleString";
        public static readonly byte[] SampleBytes = Encoding.UTF8.GetBytes(Sample);

        [GlobalSetup]
        public void GlobalSetup()
        {
//            _data = ReadDataFile("javaout");
        }


        [Benchmark]
        public void GetByteCountDecode()
        {
            var sample = Encoding.UTF8.GetString(SampleBytes);
            var bc = Encoding.UTF8.GetByteCount(sample);
        }

        [Benchmark]
        public void GetByteCountSpan()
        {
            var bc = Encoding.UTF8.GetByteCount(Sample);
        }

//        [Benchmark]
//        public void ReadStringNew()
//        {
//            using (var reader = new UserBinaryReader(new MemoryStream(_data)))
//            {
//                reader.Next();
//                reader.StepIn();
//                while (reader.Next() != IonType.None)
//                {
//                    //load the value
//                    reader.StringValue();
//                }
//
//                reader.StepOut();
//            }
//        }
//
//        [Benchmark]
//        public void Baseline()
//        {
//            using (var reader = new UserBinaryReader(new MemoryStream(_data)))
//            {
//                reader.Next();
//            }
//        }

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

    internal class Block<T> : ReadOnlySequenceSegment<T>
    {
        public Block(T[] data)
        {
            Memory = new ReadOnlyMemory<T>(data);
            RunningIndex = 0;
        }

        public void SetNext(Block<T> next)
        {
            Next = next;
        }
    }

    internal static class Program
    {
        public static void Main(string[] args)
        {
//            BenchmarkRunner.Run<Benchmarks>();
            var seg1 = new Block<char>(new[] {'1'});
            var seg2 = new Block<char>(new[] {'2', '3'});
            seg1.SetNext(seg2);
            var seg = new ReadOnlySequence<char>(seg1, 0, seg2, 2);
            Console.WriteLine(seg.Length);
            foreach (var sm in seg)
            {
                for (var i = 0; i < sm.Length; i++)
                {
                    Console.WriteLine(sm.Span[i]);
                }
            }
        }
    }
}
