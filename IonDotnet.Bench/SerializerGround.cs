using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using IonDotnet.Conversions;
using IonDotnet.Internals;
using IonDotnet.Internals.Binary;
using IonDotnet.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace IonDotnet.Bench
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class SerializerGround : IRunable
    {
        private class TimeSpanConverter : IScalarWriter, IScalarConverter
        {
            public bool TryWriteValue<T>(IValueWriter valueWriter, T value)
            {
                switch (value)
                {
                    case TimeSpan timeSpan:
                        valueWriter.SetTypeAnnotation("days");
                        valueWriter.WriteInt(timeSpan.Days);
                        return true;
                    default:
                        return false;
                }
            }

            private string _timeSpanKind;

            public void OnValueStart()
            {
            }

            public void OnValueEnd()
            {
            }

            public void OnSymbol(in SymbolToken symbolToken)
            {
                _timeSpanKind = symbolToken.Text;
            }

            public bool TryConvertTo(Type targetType, in ValueVariant valueVariant, out object result)
            {
                if (targetType != typeof(TimeSpan))
                {
                    result = default;
                    return false;
                }

                switch (_timeSpanKind)
                {
                    default:
                        result = default;
                        return false;
                    case "years":
                        result = new TimeSpan(valueVariant.IntValue * 365, 0, 0, 0);
                        return true;
                    case "days":
                        result = new TimeSpan(valueVariant.IntValue, 0, 0, 0);
                        return true;
                }
            }
        }

        public enum ExperimentResult
        {
            Success,
            Failure,
            Unknown
        }

        private class Experiment
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public TimeSpan Duration { get; set; }
            public bool IsActive { get; set; }
            public byte[] SampleData { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            public ExperimentResult Result { get; set; }
        }

        [MemoryDiagnoser]
        public class Benchmark
        {
            private static readonly List<Experiment> Data = GenerateArray();
            private const int Times = 1000;

            private static List<Experiment> GenerateArray()
            {
                var random = new Random();
                var l = new List<Experiment>();
                for (var i = 0; i < Times; i++)
                {
                    l.Add(new Experiment
                    {
                        Name = $"Bob{i}",
//                        Age = random.Next(0, 1000000),
                        Description = Guid.NewGuid().ToString(),
                        IsActive = true,
                        Id = random.Next(0, 1000000)
                    });
                }

                return l;
            }

            private readonly IonSerialization _serializer = new IonSerialization();

            [Benchmark]
            public int JsonDotnet()
            {
                var s = JsonConvert.SerializeObject(Data);
                return Encoding.UTF8.GetBytes(s).Length;
            }

            [Benchmark]
            public int IonDotnet()
            {
                var b = IonSerialization.Serialize(Data);
                return b.Length;
            }

            private static readonly IIonWriter Writer = new ManagedBinaryWriter(IonConstants.EmptySymbolTablesArray);

            [Benchmark]
            public void IonDotnetManual()
            {
                //                _serializer.Serialize(Data);
                //                using (var stream = new MemoryStream())
                //                {
                Writer.StepIn(IonType.List);
                foreach (var poco in Data)
                {
                    Writer.StepIn(IonType.Struct);

//                    Writer.SetFieldName("Age");
//                    Writer.WriteInt(poco.Age);
//                    Writer.SetFieldName("Name");
//                    Writer.WriteString(poco.Name);
//                    Writer.SetFieldName("Nickname");
//                    Writer.WriteString(poco.Nickname);
//                    Writer.SetFieldName("IsHandsome");
//                    Writer.WriteBool(poco.IsHandsome);
//                    Writer.SetFieldName("Id");
//                    Writer.WriteInt(poco.Id);

                    Writer.StepOut();
                }

                Writer.StepOut();
                //                        writer.Finish(stream);

                //                }
            }
        }

        private static string GetJson(string api)
        {
            using (var httpClient = new HttpClient())
            {
                var str = httpClient.GetStringAsync(api);
                str.Wait();
                return str.Result;
            }
        }

        private static byte[] Compress(byte[] data)
        {
            using (var memStream = new MemoryStream())
            {
                using (var compressionStream = new GZipStream(memStream, CompressionLevel.NoCompression))
                {
                    compressionStream.Write(data);
                }

                var compressed = memStream.ToArray();
                return compressed;
            }
        }

        public void Run(string[] args)
        {
//            BenchmarkRunner.Run<Benchmark>();
//            var jsonString = GetJson(@"https://api.foursquare.com/v2/venues/explore?near=NYC
//                &oauth_token=IRLTRG22CDJ3K2IQLQVR1EP4DP5DLHP343SQFQZJOVILQVKV&v=20180728");
//
//            var obj = JsonConvert.DeserializeObject<RootObject>(jsonString);
//            var jsonBytes = Encoding.UTF8.GetBytes(jsonString);
//            var ionBytes = IonSerialization.Serialize(obj);
//
//            Console.WriteLine($"JSON size: {jsonBytes.Length}");
//            Console.WriteLine($"ION size: {ionBytes.Length}");
//
//            var compressedJson = Compress(jsonBytes);
//            var compressedIon = Compress(ionBytes);
//            Console.WriteLine($"compressed JSON size: {compressedJson.Length}");
//            Console.WriteLine($"compressed ION size: {compressedIon.Length}");
            var exp = new Experiment
            {
                Name = "Boxing Perftest",
                Duration = TimeSpan.FromSeconds(30),
                Id = 233,
                IsActive = true,
                Description = "Measure performance impact of boxing",
                Result = ExperimentResult.Success,
                SampleData = new byte[100]
            };
            Array.Fill(exp.SampleData, (byte) 1);
            var converter = new TimeSpanConverter();
            var b = IonSerialization.Serialize(exp, converter);
            var d = IonSerialization.Deserialize<Experiment>(b, converter);

            Console.WriteLine(JsonConvert.SerializeObject(d, Formatting.Indented));
            Console.WriteLine(typeof(IonType).IsValueType);
        }
    }
}
