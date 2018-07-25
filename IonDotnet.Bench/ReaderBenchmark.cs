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
            private const string SomeString = nameof(SomeString);
            private readonly byte[] _data = DirStructure.ReadDataFile("javaout");

            [Benchmark]
            public void ReadStringLoadValue()
            {
                var reader = new UserBinaryReader(new MemoryStream(_data));

                reader.MoveNext();
                reader.StepIn();
                while (reader.MoveNext() != IonType.None)
                {
                    //load the value
                    reader.StringValue();
                }

                reader.StepOut();
            }

            [Benchmark]
            public void ReadStringNoLoadValue()
            {
                var reader = new UserBinaryReader(new MemoryStream(_data));
                reader.MoveNext();
                reader.StepIn();
                while (reader.MoveNext() != IonType.None)
                {
                    //do nothing
                }

                reader.StepOut();
            }

            [Benchmark]
            public void Baseline()
            {
                var reader = new UserBinaryReader(new MemoryStream(_data));
                reader.MoveNext();
            }

            [Benchmark]
            public void CompStruct()
            {
                var s = new SymbolToken(SomeString, 213123213);
                var eq = s == default;
            }
        }

        public void Run(ArraySegment<string> args)
        {
            BenchmarkRunner.Run<CompareReader>();
        }
    }
}
