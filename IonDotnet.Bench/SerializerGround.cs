using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using IonDotnet.Serialization;
using Newtonsoft.Json;

namespace IonDotnet.Bench
{
    // ReSharper disable once UnusedMember.Global
    public class SerializerGround : IRunable
    {
        public class SimplePoco
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
            public static SimplePoco[] Data = GenerateArray();

            public static SimplePoco[] GenerateArray()
            {
                var random = new Random();
                return new[]
                {
                    new SimplePoco
                    {
                        Name = "Bob",
                        Age = random.Next(0, int.MaxValue / 5),
                        Nickname = Guid.NewGuid().ToString(),
                        IsHandsome = true,
                        Id = random.Next(0, int.MaxValue / 3)
                    },
                    new SimplePoco
                    {
                        Name = "Adam",
                        Age = random.Next(0, int.MaxValue / 5),
                        Nickname = Guid.NewGuid().ToString(),
                        IsHandsome = false,
                        Id = random.Next(0, int.MaxValue / 3)
                    },
                    new SimplePoco
                    {
                        Name = "Jason",
                        Age = random.Next(0, int.MaxValue / 5),
                        Nickname = Guid.NewGuid().ToString(),
                        IsHandsome = false,
                        Id = random.Next(0, int.MaxValue / 3)
                    },
                    new SimplePoco
                    {
                        Name = "Helen",
                        Age = random.Next(0, int.MaxValue / 5),
                        Nickname = Guid.NewGuid().ToString(),
                        IsHandsome = true,
                        Id = random.Next(0, int.MaxValue / 3)
                    }
                };
            }

            [Benchmark]
            public void JsonDotnet()
            {
                JsonConvert.SerializeObject(Data);
            }

            [Benchmark]
            public void IonDotnet()
            {
                var serializer = new IonSerializer();
                serializer.Serialize(Data);
            }
        }

        public void Run(ArraySegment<string> args)
        {
            BenchmarkRunner.Run<Benchmark>();
//            var serializer = new IonSerializer();
//
//            var listPoco =
//                var output = serializer.Serialize(listPoco);
////            Console.WriteLine(string.Join(",", output.Select(b => $"0x{b:x2}")));
//
//            var jsonOutput = JsonConvert.SerializeObject(listPoco);
//            Console.WriteLine(jsonOutput);
//            var jsonBytes = Encoding.UTF8.GetBytes(jsonOutput);
//            Console.WriteLine($"json Length {jsonBytes.Length}");
//            Console.WriteLine($"ion length {output.Length}");
        }
    }
}
