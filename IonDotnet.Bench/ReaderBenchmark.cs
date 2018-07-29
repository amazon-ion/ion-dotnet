using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using IonDotnet.Internals;
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
                IonSerialization.Serialize(Objs);
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
                    var ioned = IonSerialization.Serialize(Objs);
                    Console.WriteLine(ioned.Length);
                }
            }
        }

        public void Run(string[] args)
        {
            const string fn = "dotnetout";
            const int defaultDepth = 400;

            var depth = args.Length > 1 ? int.Parse(args[1]) : defaultDepth;
//            var s = "layer182";
//            var bytes = Encoding.UTF8.GetBytes(s);
//            var sback = Encoding.UTF8.GetString(bytes);
//            Console.WriteLine(sback);
            WriteLayersDeep(fn, depth);

//            var data = DirStructure.ReadDataFile("javaout");
            var data = DirStructure.ReadDataFile(fn);
            var reader = new UserBinaryReader(new MemoryStream(data));
            for (var i = 0; i < depth; i++)
            {
                try
                {
                    var readType = reader.MoveNext();
                    if (IonType.Struct != readType)
                    {
                        throw new Exception($"type is {readType}");
                    }

                    reader.StepIn();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"exception at idx {i}");
                    Console.WriteLine($"{e.GetType().Name}: {e.Message}");
                    Console.WriteLine(e.StackTrace);
                    break;
                }
            }
        }

        private static void WriteLayersDeep(string fileName, int depth)
        {
            using (var writer = new ManagedBinaryWriter(IonConstants.EmptySymbolTablesArray))
            {
                writer.StepIn(IonType.Struct);
                for (var i = 0; i < depth - 1; i++)
                {
                    writer.SetFieldName($"layer{i}");
                    writer.StepIn(IonType.Struct);
                }

                for (var i = 0; i < depth; i++)
                {
                    writer.StepOut();
                }

                using (var fs = DirStructure.OpenWrite(fileName))
                {
                    writer.Flush(fs);
                }
            }
        }
    }
}
