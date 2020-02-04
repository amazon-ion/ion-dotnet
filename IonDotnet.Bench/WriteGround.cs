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
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using IonDotnet.Builders;
using IonDotnet.Serialization;
using Newtonsoft.Json;

namespace IonDotnet.Bench
{
    // ReSharper disable once UnusedMember.Global
    public class WriteGround : IRunable
    {
        [MemoryDiagnoser]
        public class SerBenchmark
        {
            private static readonly Experiment Exp = new Experiment
            {
                Name = "Boxing Perftest",
                // Duration = TimeSpan.FromSeconds(90),
                Id = 233,
                StartDate = new DateTimeOffset(2018, 07, 21, 11, 11, 11, TimeSpan.Zero),
                IsActive = true,
                Description = "Measure performance impact of boxing",
                Result = ExperimentResult.Failure,
                SampleData = new byte[10],
                Budget = decimal.Parse("12345.01234567890123456789"),
                Outputs = new[] {1.2, 2.3, 3.1}
            };

            [Benchmark]
            public void JsonDotnetToString()
            {
                JsonConvert.SerializeObject(Exp);
            }

            [Benchmark]
            public void IonDotnetText()
            {
                IonExpressionText.Serialize(Exp);
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void RunBenchmark()
        {
            BenchmarkRunner.Run<SerBenchmark>();
        }

        public void Run(string[] args)
        {
            var experiment = new Experiment
            {
                Name = "Boxing Perftest",
                // Duration = TimeSpan.FromSeconds(90),
                Id = 233,
                StartDate = new DateTimeOffset(2018, 07, 21, 11, 11, 11, TimeSpan.Zero),
                IsActive = true,
                Description = "Measure performance impact of boxing",
                Result = ExperimentResult.Failure,
                SampleData = new byte[10],
                Budget = decimal.Parse("12345.01234567890123456789"),
                Outputs = new[] {1.2, 2.3, 3.1}
            };

            //Serialize an object to byte array
            byte[] ionBytes = IonSerialization.Binary.Serialize(experiment);

            //Deserialize a byte array to an object
            Experiment deserialized = IonSerialization.Binary.Deserialize<Experiment>(ionBytes);

            //Serialize an object to string
            string text = IonSerialization.Text.Serialize(experiment, new IonTextOptions {PrettyPrint = true});

            //Deserialize a string to an object
            deserialized = IonSerialization.Text.Deserialize<Experiment>(text);

            Console.WriteLine(text);
        }
    }
}
