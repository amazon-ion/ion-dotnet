using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using IonDotnet.Conversions;
using IonDotnet.Serialization;
using Newtonsoft.Json;
// ReSharper disable All

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

        public abstract class Benchmark
        {
            protected static readonly List<Experiment> Data = GenerateArray();
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
                        Budget = decimal.Parse("12345.01234567890123456789", System.Globalization.CultureInfo.InvariantCulture)
                    });
                }

                return l;
            }
        }

        [MemoryDiagnoser]
        public class BinaryBenchmark : Benchmark
        {
            private static readonly JsonSerializer JSerializer = new JsonSerializer();

            [Benchmark]
            public byte[] JsonDotnetBytes()
            {
                using (var memStream = new MemoryStream())
                {
                    using (var streamWriter = new StreamWriter(memStream, Encoding.UTF8))
                    {
                        JSerializer.Serialize(streamWriter, Data);
                        return memStream.ToArray();
                    }
                }
            }


            [Benchmark]
            public byte[] IonDotnetExpBinary()
            {
                return IonExpressionBinary.Serialize(Data);
            }
        }

        [MemoryDiagnoser]
        public class TextBenchmark : Benchmark
        {
            [Benchmark]
            public string IonDotnetText()
            {
                return IonExpressionText.Serialize(Data);
            }

            [Benchmark]
            public string JsonDotnetString()
            {
                return JsonConvert.SerializeObject(Data);
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
            BenchmarkRunner.Run<TextBenchmark>();
            BenchmarkRunner.Run<BinaryBenchmark>();
        }
    }
}
