using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using IonDotnet.Conversions;
using IonDotnet.Internals;
using IonDotnet.Internals.Binary;
using IonDotnet.Internals.Text;
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
                        if (timeSpan.Days > 0)
                        {
                            valueWriter.SetTypeAnnotation("days");
                            valueWriter.WriteFloat(timeSpan.TotalDays);
                        }
                        else if (timeSpan.Hours > 0)
                        {
                            valueWriter.SetTypeAnnotation("hours");
                            valueWriter.WriteFloat(timeSpan.TotalHours);
                        }
                        else if (timeSpan.Minutes > 0)
                        {
                            valueWriter.SetTypeAnnotation("minutes");
                            valueWriter.WriteFloat(timeSpan.TotalMinutes);
                        }
                        else if (timeSpan.Seconds > 0)
                        {
                            valueWriter.SetTypeAnnotation("seconds");
                            valueWriter.WriteFloat(timeSpan.TotalSeconds);
                        }
                        else if (timeSpan.Milliseconds > 0)
                        {
                            valueWriter.SetTypeAnnotation("millis");
                            valueWriter.WriteFloat(timeSpan.TotalMilliseconds);
                        }
                        else
                        {
                            valueWriter.SetTypeAnnotation("ticks");
                            valueWriter.WriteInt(timeSpan.Ticks);
                        }

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

            public void OnAnnotation(in SymbolToken symbolToken)
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
                    case "days":
                        result = TimeSpan.FromDays(valueVariant.DoubleValue);
                        return true;
                    case "hours":
                        result = TimeSpan.FromHours(valueVariant.DoubleValue);
                        return true;
                    case "minutes":
                        result = TimeSpan.FromMinutes(valueVariant.DoubleValue);
                        return true;
                    case "seconds":
                        result = TimeSpan.FromSeconds(valueVariant.DoubleValue);
                        return true;
                    case "millis":
                        result = TimeSpan.FromMilliseconds(valueVariant.DoubleValue);
                        return true;
                    case "ticks":
                        result = TimeSpan.FromTicks(valueVariant.LongValue);
                        return true;
                }
            }
        }

        [MemoryDiagnoser]
        public class Benchmark
        {
            private static readonly List<Experiment> Data = GenerateArray();
            private const int Times = 1000;

            private static List<Experiment> GenerateArray()
            {
                var l = new List<Experiment>();
                for (var i = 0; i < Times; i++)
                {
                    l.Add(new Experiment
                    {
                        Name = "Boxing Perftest",
                        // Duration = TimeSpan.FromSeconds(90),
                        Id = 233,
                        StartDate = new DateTimeOffset(2018, 07, 21, 11, 11, 11, TimeSpan.Zero),
                        IsActive = true,
                        Description = "Measure performance impact of boxing",
                        Result = ExperimentResult.Failure,
                        SampleData = new byte[100],
                        Budget = decimal.Parse("12345.01234567890123456789")
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
            public void IonDotnetExp()
            {
                IonSerializerExpression.Serialize(Data);
            }

            [Benchmark]
            public void IonDotnetReflection()
            {
                IonSerialization.Serialize(Data);
            }

            public static readonly IIonWriter Writer = new ManagedBinaryWriter(BinaryConstants.EmptySymbolTablesArray);

            [Benchmark]
            public void IonDotnetManual()
            {
                //                _serializer.Serialize(Data);
                //                using (var stream = new MemoryStream())
                //                {
                byte[] bytes = null;
                Writer.StepIn(IonType.List);
                foreach (var poco in Data)
                {
                    Writer.StepIn(IonType.Struct);

                    Writer.SetFieldName("Id");
                    Writer.WriteInt(poco.Id);
                    Writer.SetFieldName("Name");
                    Writer.WriteString(poco.Name);
                    Writer.SetFieldName("Description");
                    Writer.WriteString(poco.Description);
                    Writer.SetFieldName("StartDate");
                    Writer.WriteTimestamp(new Timestamp(poco.StartDate));
                    Writer.SetFieldName("IsActive");
                    Writer.WriteBool(poco.IsActive);
                    Writer.SetFieldName("SampleData");
                    Writer.WriteBlob(poco.SampleData);
                    Writer.SetFieldName("Budget");
                    Writer.WriteDecimal(poco.Budget);

                    Writer.StepOut();
                }

                Writer.StepOut();
                Writer.Flush(ref bytes);
                Writer.Finish();

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
            var data = DirStructure.ReadDataFile("javaout.ion");
            var str = Encoding.ASCII.GetString(data);
            var sq = new CharSequenceStream(str);
            var reader = new SystemTextReader(sq);
            Console.WriteLine(reader.MoveNext());
            reader.StepIn();

            Console.WriteLine(reader.MoveNext());
            Console.WriteLine(reader.CurrentFieldName);
            Console.WriteLine(reader.IntValue());

            Console.WriteLine(reader.MoveNext());
            Console.WriteLine(reader.CurrentFieldName);
            Console.WriteLine(reader.StringValue());
            
            
            Console.WriteLine(reader.MoveNext());
            Console.WriteLine(reader.CurrentFieldName);
            Console.WriteLine(reader.BoolValue());
//            BenchmarkRunner.Run<Benchmark>();
            //warmup
            // var sw = new Stopwatch();
            // sw.Start();
            // sw.Stop();
            // for (var i = 0; i < 1000; i++)
            // {
            //     RunOnce();
            // }

            // sw.Start();

            // for (var i = 0; i < 1000; i++)
            // {
            //     RunOnce();
            // }

            // sw.Stop();
            // Console.WriteLine($"IonDotnet: writing took {sw.ElapsedTicks * 1.0 / TimeSpan.TicksPerMillisecond}ms");
        }

        private static void RunOnce()
        {
            Benchmark.Writer.StepIn(IonType.List);
            for (var i = 0; i < 1000; i++)
            {
                Benchmark.Writer.StepIn(IonType.Struct);

                Benchmark.Writer.SetFieldName("boolean");
                Benchmark.Writer.WriteBool(true);
                Benchmark.Writer.SetFieldName("string");
                Benchmark.Writer.WriteString("this is a string");
                Benchmark.Writer.SetFieldName("integer");
                Benchmark.Writer.WriteInt(int.MaxValue);
                Benchmark.Writer.SetFieldName("float");
                Benchmark.Writer.WriteFloat(432.23123f);
                Benchmark.Writer.SetFieldName("timestamp");
                Benchmark.Writer.WriteTimestamp(new Timestamp(new DateTime(2000, 11, 11)));

                Benchmark.Writer.StepOut();
            }

            byte[] bytes = null;
            Benchmark.Writer.StepOut();
            Benchmark.Writer.Flush(ref bytes);
            Benchmark.Writer.Finish();
        }
    }
}
