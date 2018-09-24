using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace IonDotnet.Bench
{
    // ReSharper disable once UnusedMember.Global
    public class BinaryWriterGround : IRunable
    {
        [MemoryDiagnoser]
        public class Benchmark
        {
            [Benchmark]
            public void ToArray()
            {
                using (var stream = new MemoryStream())
                {
                    Write1000(stream);
                    stream.ToArray();
                }
            }

            [Benchmark]
            public void GetBuffer()
            {
                using (var stream = new MemoryStream())
                {
                    stream.WriteByte(45);
                    stream.Capacity = 1000;
                    stream.Seek(0, SeekOrigin.Begin);
                    Write1000(stream);

                    var buffer = stream.GetBuffer();
                    if (buffer.Length != 1000 || stream.Length != 1000)
                        throw new Exception();
                }
            }

            private static void Write1000(Stream stream)
            {
                for (var i = 0; i < 1000; i++)
                {
                    stream.WriteByte(46);
                }
            }
        }

        public void Run(string[] args)
        {
            BenchmarkRunner.Run<Benchmark>();
        }
    }
}
