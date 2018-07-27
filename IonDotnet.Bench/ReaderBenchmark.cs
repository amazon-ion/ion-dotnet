using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using IonDotnet.Internals.Binary;
using IonDotnet.Serialization;
using Newtonsoft.Json;

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

        [MemoryDiagnoser]
        public class CompareWriter
        {
            private static readonly List<RootObject> Objs;
            private const int Times = 1;

            static CompareWriter()
            {
                var jsonBytes = DirStructure.ReadDataFile("sample.json");
                var jsonString = Encoding.UTF8.GetString(jsonBytes);
                Objs = JsonConvert.DeserializeObject<List<RootObject>>(jsonString);
                JsonConvert.SerializeObject(Objs);
                IonSerializer.Serialize(Objs);
            }

            [Benchmark]
            public void Json()
            {
                for (var i = 0; i < Times; i++)
                {
                    var jsoned = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Objs));
                    Console.WriteLine(jsoned.Length);
                }
            }

            [Benchmark]
            public void Ion()
            {
                for (var i = 0; i < Times; i++)
                {
                    var ioned = IonSerializer.Serialize(Objs);
                    Console.WriteLine(ioned.Length);
                }
            }
        }

        public void Run(string[] args)
        {
            var decBin = DirStructure.ReadDataFile("javaout");
            var reader = new UserBinaryReader(new MemoryStream(decBin));
            Console.WriteLine(reader.MoveNext());
            Console.WriteLine(reader.DecimalValue());

//            var b = new CompareWriter();
//            b.Json();
//            b.Ion();
//            BenchmarkRunner.Run<CompareWriter>();
        }
    }
}
