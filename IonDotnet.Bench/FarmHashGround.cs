using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace IonDotnet.Bench
{
    // ReSharper disable once ClassNeverInstantiated.Global
    [MemoryDiagnoser]
    public class FarmHashGround : IRunable
    {
        public class Benchmark
        {
            private static readonly string[] SampleStrings = GenerateSamples();

            private static readonly IReadOnlyCollection<string> RocStrings = SampleStrings;

            private static string[] GenerateSamples()
            {
                var s = new string[1000];
                for (var i = 0; i < s.Length; i++)
                {
                    s[i] = Guid.NewGuid().ToString();
                }

                return s;
            }

            [Benchmark]
            public void ReadOnlyCollection()
            {
                foreach (var s in RocStrings)
                {
                    var ss = s;
                }
            }

            [Benchmark]
            public void Array()
            {
                foreach (var s in SampleStrings)
                {
                    var ss = s;
                }
            }

//            [Benchmark]
//            public void FarmHash()
//            {
//                var hs = (int) Farmhash.Sharp.Farmhash.Hash32(SampleStrings[0]);
////                foreach (var str in SampleStrings)
////                {
////                    var hash = (int) Farmhash.Sharp.Farmhash.Hash32(str);
////                }
//            }
//
//            [Benchmark]
//            public void DefaultHashCode()
//            {
//                var hs = SampleStrings[0].GetHashCode();
////                foreach (var str in SampleStrings)
////                {
////                    var hash = str.GetHashCode();
////                }
//            }
        }

        public void Run(string[] args)
        {
            BenchmarkRunner.Run<Benchmark>();
        }

        private void ProcessStrings(string[] strings)
        {
            for (var i = 0; i < strings.Length; i++)
            {
                var t = 3;
            }
        }
    }
}
