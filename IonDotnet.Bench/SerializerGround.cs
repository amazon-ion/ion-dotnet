using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
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
    public class SerializerGround : IRunable
    {
        private class SimplePoco
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public string Nickname { get; set; }
            public int Id { get; set; }
            public bool IsHandsome { get; set; }
        }

        [MemoryDiagnoser]
        public class Benchmark
        {
            private static readonly List<SimplePoco> Data = GenerateArray();
            private const int Times = 100;

            private static List<SimplePoco> GenerateArray()
            {
                var random = new Random();
                var l = new List<SimplePoco>();
                for (var i = 0; i < Times; i++)
                {
                    l.Add(new SimplePoco
                    {
                        Name = $"Bob{i}",
                        Age = random.Next(0, 1000000),
                        Nickname = Guid.NewGuid().ToString(),
                        IsHandsome = true,
                        Id = random.Next(0, 1000000)
                    });
                }

                return l;
            }

            private readonly IonSerialization _serializer = new IonSerialization();

//            [Benchmark]
            public int JsonDotnet()
            {
                var s = JsonConvert.SerializeObject(Data);
                return Encoding.UTF8.GetBytes(s).Length;
            }

//            [Benchmark]
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

                    Writer.SetFieldName("Age");
                    Writer.WriteInt(poco.Age);
                    Writer.SetFieldName("Name");
                    Writer.WriteString(poco.Name);
                    Writer.SetFieldName("Nickname");
                    Writer.WriteString(poco.Nickname);
                    Writer.SetFieldName("IsHandsome");
                    Writer.WriteBool(poco.IsHandsome);
                    Writer.SetFieldName("Id");
                    Writer.WriteInt(poco.Id);

                    Writer.StepOut();
                }

                Writer.StepOut();
                //                        writer.Finish(stream);

                //                }
            }
        }

        private class Person
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public int[] Ids { get; set; }
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
            var jsonString = GetJson(@"https://api.foursquare.com/v2/venues/explore?near=NYC
                &oauth_token=IRLTRG22CDJ3K2IQLQVR1EP4DP5DLHP343SQFQZJOVILQVKV&v=20180728");

            var obj = JsonConvert.DeserializeObject<RootObject>(jsonString);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonString);
            var ionBytes = IonSerialization.Serialize(obj);

            Console.WriteLine($"JSON size: {jsonBytes.Length}");
            Console.WriteLine($"ION size: {ionBytes.Length}");

            var compressedJson = Compress(jsonBytes);
            var compressedIon = Compress(ionBytes);
            Console.WriteLine($"compressed JSON size: {compressedJson.Length}");
            Console.WriteLine($"compressed ION size: {compressedIon.Length}");
        }
    }
}
