using System;

namespace IonDotnet.Systems
{
    public class IonTextOptions
    {
        public static readonly IonTextOptions Default = new IonTextOptions();

        private string _lineSeparator;

        public bool PrettyPrint { get; set; }

        public string LineSeparator { get; set; } = Environment.NewLine;

        public bool SymbolAsString { get; set; }

        public bool StringAsJson { get; set; }

        public bool SkipAnnotations { get; set; }

        public bool UntypedNull { get; set; }

        public bool TimestampAsMillis { get; set; }

        public int LongStringThreshold { get; set; } = 64;

        public bool WriteVersionMarker { get; set; }
    }
}
