using System;
using System.IO;
using IonDotnet.Internals.Text;
using IonDotnet.Serialization;
using IonDotnet.Systems;

namespace IonDotnet.Bench
{
    // ReSharper disable once UnusedMember.Global
    public class WriteGround : IRunable
    {
        public void Run(string[] args)
        {
            var exp = new Experiment
            {
                Name = "Boxing Perftest",
                // Duration = TimeSpan.FromSeconds(90),
                Id = 233,
                StartDate = new DateTimeOffset(2018, 07, 21, 11, 11, 11, TimeSpan.Zero),
                IsActive = true,
                Description = "Measure performance impact of boxing",
                Result = ExperimentResult.Failure,
                SampleData = new byte[100],
                Budget = decimal.Parse("12345.01234567890123456789"),
                Outputs = new[] {1, 2, 3}
            };

            var text = IonSerialization.Text.Serialize(exp, new IonTextOptions {PrettyPrint = true});
            Console.WriteLine(text);
            var obj = IonSerialization.Text.Deserialize<Experiment>(text);
        }
    }
}
