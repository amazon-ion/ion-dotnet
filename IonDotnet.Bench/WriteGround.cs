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
            var experiment = new Experiment
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
