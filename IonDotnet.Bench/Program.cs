using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace IonDotnet.Bench
{
    [MemoryDiagnoser]
    public class Benchmarks
    {
        public List<Memory<byte>> LL;

//        public List<string> _data;
        private const int Iter = 500;
        private readonly List<string> _stringsss = new List<string>();
        private readonly List<string> _s2 = new List<string>();

        [GlobalSetup]
        public void GlobalSetup()
        {
            for (var i = 0; i < Iter; i++)
            {
                _stringsss.Add($"Stringsss{i}");
            }

            for (var i = 0; i < Iter * 2; i++)
            {
                _s2.Add($"Stringsss{i}");
            }
        }


        [Benchmark]
        public void GenericLinkedList()
        {
            var ll1 = new MyLinkedList<string>();
            foreach (var s in _stringsss)
            {
                ll1.Add(s);
            }

            var ll2 = new MyLinkedList<string>();
            foreach (var s in _s2)
            {
                ll2.Add(s);
            }

            var ll3 = new MyLinkedList<string>();
            foreach (var s in _s2)
            {
                ll3.Add(s);
            }

            ll1.Append(ll2);
            ll1.Append(ll3);

            foreach (var s in ll1)
            {
                var x = s;
            }
        }

        [Benchmark]
        public void NetLinkedList()
        {
            var ll1 = new LinkedList<string>();
            foreach (var s in _stringsss)
            {
                ll1.AddLast(s);
            }

            var ll2 = new LinkedList<string>();
            foreach (var s in _s2)
            {
                ll2.AddLast(s);
            }

            var ll3 = new LinkedList<string>();
            foreach (var s in _s2)
            {
                ll3.AddLast(s);
            }

            foreach (var s2 in ll2)
            {
                ll1.AddLast(s2);
            }

            foreach (var s3 in ll3)
            {
                ll1.AddLast(s3);
            }

            foreach (var s in ll1)
            {
                var x = s;
            }
        }

        [Benchmark]
        public void JustUseList()
        {
            var ll1 = new List<string>();
            foreach (var s in _stringsss)
            {
                ll1.Add(s);
            }

            var ll2 = new List<string>();
            foreach (var s in _s2)
            {
                ll2.Add(s);
            }

            var ll3 = new List<string>();
            foreach (var s in _s2)
            {
                ll3.Add(s);
            }

            ll1.AddRange(ll2);
            ll1.AddRange(ll3);
            foreach (var s in ll1)
            {
                var x = s;
            }
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
            BenchmarkRunner.Run<Benchmarks>();
//            var seg1 = new Block<char>(new[] {'1'});
//            var seg2 = new Block<char>(new[] {'2', '3'});
//            seg1.SetNext(seg2);
//            var seg = new ReadOnlySequence<char>(seg1, 0, seg2, 2);
//            Console.WriteLine(seg.Length);
//            foreach (var sm in seg)
//            {
//                for (var i = 0; i < sm.Length; i++)
//                {
//                    Console.WriteLine(sm.Span[i]);
//                }
//            }
        }
    }
}
