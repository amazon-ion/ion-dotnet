using System;
using IonDotnet.Serialization;
using Newtonsoft.Json;

namespace IonDotnet.Bench
{
    // ReSharper disable once UnusedMember.Global
    public class ExpressionExp : IRunable
    {
        public void Run(string[] args)
        {
            // var exp = typeof(IEnumerator).GetMethod(nameof(IEnumerator.MoveNext));
            // var exp2 = typeof(IValueWriter).GetMethod(nameof(IIonWriter.WriteString));
            // Console.WriteLine(exp);
            // Console.WriteLine(exp2);
            // return;

            //            var trueS = IonSerialization.Serialize(new Per
            //            {
            //                Name = "huy"
            //            });
            //            Console.WriteLine(string.Join(',', trueS.Select(b => $"{b:x2}")));
            var s = IonExpressionBinary.Serialize(new[]
            {
                new Experiment
                {
                    Name = "Boxing Perftest",
                    // Duration = TimeSpan.FromSeconds(90),
                    Id = 233,
                    StartDate = new DateTimeOffset(2018, 07, 21, 11, 11, 11, TimeSpan.Zero),
                    IsActive = true,
                    Description = "Measure performance impact of boxing",
                    Result = ExperimentResult.Failure,
                    SampleData = new byte[100],
                    Budget = decimal.Parse("12345.01234567890123456789", System.Globalization.CultureInfo.InvariantCulture),
                    Outputs = new[] {1.2, 2.3, 3.1}
                }
            });

            var s2 = IonExpressionBinary.Serialize(new[]
            {
                new Experiment
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
                }
            });
            //            Console.WriteLine(string.Join(',', s.Select(b => $"{b:x2}")));
            var d = IonSerialization.Binary.Deserialize<Experiment[]>(s);
            var json = JsonConvert.SerializeObject(d, Formatting.Indented);
            Console.WriteLine(json);
        }
    }
}
