using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using IonDotnet.Internals.Binary;

namespace IonDotnet.Bench
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ReaderBenchmark : IRunable
    {
        [MemoryDiagnoser]
        public class CompareReader
        {
            private readonly byte[] _data = DirStructure.ReadDataFile("javaout");

            [Benchmark]
            public void ReadStringLoadValue()
            {
                using (var reader = new UserBinaryReader(new MemoryStream(_data)))
                {
                    reader.MoveNext();
                    reader.StepIn();
                    while (reader.MoveNext() != IonType.None)
                    {
                        //load the value
                        reader.StringValue();
                    }

                    reader.StepOut();
                }
            }
            
            [Benchmark]
            public void ReadStringNoLoadValue()
            {
                using (var reader = new UserBinaryReader(new MemoryStream(_data)))
                {
                    reader.MoveNext();
                    reader.StepIn();
                    while (reader.MoveNext() != IonType.None)
                    {
                        //do nothing
                    }

                    reader.StepOut();
                }
            }

            [Benchmark]
            public void Baseline()
            {
                using (var reader = new UserBinaryReader(new MemoryStream(_data)))
                {
                    reader.MoveNext();
                }
            }
        }

        public void Run(ArraySegment<string> args)
        {
            BenchmarkRunner.Run<CompareReader>();
        }
    }
}
