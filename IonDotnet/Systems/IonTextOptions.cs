using System;

namespace IonDotnet.Systems
{
    public class IonTextOptions
    {
        public IonTextOptions()
        {
            LongStringThreshold = 30;
        }

        private string _lineSeparator;

        public bool PrettyPrint { get; set; }

        public string LineSeparator
        {
            get => _lineSeparator ?? Environment.NewLine;
            set => _lineSeparator = value;
        }

        public bool SymbolAsString { get; set; }

        public bool StringAsJson { get; set; }

        public bool SkipAnnotations { get; set; }

        public bool UntypedNull { get; set; }

        public bool TimestampAsMillis { get; set; }

        public int LongStringThreshold { get; set; }
    }
}
