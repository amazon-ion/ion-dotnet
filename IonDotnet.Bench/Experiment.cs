using System;

namespace IonDotnet.Bench
{
    public enum ExperimentResult
    {
        Success,
        Failure,
        Unknown
    }

    public class Experiment
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public DateTimeOffset StartDate { get; set; }

        // public TimeSpan Duration { get; set; }
        public bool IsActive { get; set; }
        public byte[] SampleData { get; set; }
        public decimal Budget { get; set; }

        // [JsonConverter(typeof(StringEnumConverter))]
        public ExperimentResult Result { get; set; }

        public double[] Outputs { get; set; }
    }
}
