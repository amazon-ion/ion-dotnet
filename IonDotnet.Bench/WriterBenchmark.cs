using System;
using System.Diagnostics;
using System.IO;
using IonDotnet.Internals.Binary;
using IonDotnet.Utils;

// ReSharper disable UnusedMember.Global
namespace IonDotnet.Bench
{
    public class WriterBenchmark : IRunable
    {
        public void Run(string[] args)
        {
            //warmup
            var sw = new Stopwatch();
            sw.Start();
            sw.Stop();
            for (var i = 0; i < 1000; i++)
            {
                RunOnce();
            }

            sw.Start();

            for (var i = 0; i < 1000; i++)
            {
                RunOnce();
            }

            sw.Stop();
            Console.WriteLine($"IonDotnet: writing took {sw.ElapsedTicks * 1.0 / TimeSpan.TicksPerMillisecond}ms");
        }

        private static void RunOnce()
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new ManagedBinaryWriter(stream, Symbols.EmptySymbolTablesArray))
                {
                    writer.StepIn(IonType.List);
                    for (var i = 0; i < 1000; i++)
                    {
                        writer.StepIn(IonType.Struct);

                        writer.SetFieldName("boolean");
                        writer.WriteBool(true);
                        writer.SetFieldName("string");
                        writer.WriteString("this is a string");
                        writer.SetFieldName("integer");
                        writer.WriteInt(int.MaxValue);
                        writer.SetFieldName("float");
                        writer.WriteFloat(432.23123f);
                        writer.SetFieldName("timestamp");
                        writer.WriteTimestamp(new Timestamp(new DateTime(2000, 11, 11)));

                        writer.StepOut();
                    }

                    writer.StepOut();
                    writer.Flush();
                }
            }
        }
    }
}
