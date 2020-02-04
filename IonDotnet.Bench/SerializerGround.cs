/*
 * Copyright 2020 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Newtonsoft.Json;

// ReSharper disable All

namespace IonDotnet.Bench
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class SerializerGround : IRunable
    {
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
