using System;
using System.Diagnostics;
using IonDotnet.Internals.Binary;

// ReSharper disable UnusedMember.Global
namespace IonDotnet.Bench
{
    public class WriterBenchmark : IRunable
    {
        private static readonly ManagedBinaryWriter Writer = new ManagedBinaryWriter(BinaryConstants.EmptySymbolTablesArray);

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
            Writer.StepIn(IonType.List);
            for (var i = 0; i < 1000; i++)
            {
                Writer.StepIn(IonType.Struct);

                Writer.SetFieldName("boolean");
                Writer.WriteBool(true);
                Writer.SetFieldName("string");
                Writer.WriteString("this is a string");
                Writer.SetFieldName("integer");
                Writer.WriteInt(int.MaxValue);
                Writer.SetFieldName("float");
                Writer.WriteFloat(432.23123f);
                Writer.SetFieldName("timestamp");
                Writer.WriteTimestamp(new Timestamp(new DateTime(2000, 11, 11)));

                Writer.StepOut();
            }

            byte[] bytes = null;
            Writer.StepOut();
            Writer.Flush(ref bytes);
            Writer.Finish();
        }
    }
}
