using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using IonDotnet.Serialization;
using IonDotnet.Systems;
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
//            IIonWriter writer;
//            var d = -18446744073709551615m;
//            var sw = new StringWriter();
////             writer = IonTextWriterBuilder.Build(sw);
//            var s = new MemoryStream();
//            writer = IonBinaryWriterBuilder.Build(s);
//            writer.WriteDecimal(d);
//            writer.Finish();
//            var bytes = s.ToArray();
//            Console.WriteLine(string.Join(',', bytes.Select(b => $"{b:x2}")));

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
            /* Output
            {
              Id: 233,
              Name: "Boxing Perftest",
              Description: "Measure performance impact of boxing",
              StartDate: 2018-07-21T11:11:11.0000000+00:00,
              IsActive: true,
              SampleData: {{ AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA== }},
              Budget: 12345.01234567890123456789,
              Result: 'Failure',
              Outputs: [
                1,
                2,
                3
              ]
            }
            */
        }
    }
}
