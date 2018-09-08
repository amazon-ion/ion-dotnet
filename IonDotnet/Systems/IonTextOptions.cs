using System;

namespace IonDotnet.Systems
{
    public class IonTextOptions
    {
        public static readonly IonTextOptions Default = new IonTextOptions();

        /// <summary>
        /// Indented format
        /// </summary>
        public bool PrettyPrint { get; set; }

        /// <summary>
        /// New-line sequence, default to system-specific sequence
        /// </summary>
        public string LineSeparator { get; set; } = Environment.NewLine;

        /// <summary>
        /// Write symbols as strings
        /// </summary>
        public bool SymbolAsString { get; set; }

        /// <summary>
        /// Write a JSON text
        /// </summary>
        public bool Json { get; set; }

        /// <summary>
        /// Do we skip annotations?
        /// </summary>
        public bool SkipAnnotations { get; set; }

        /// <summary>
        /// All null values are written as 'null'
        /// </summary>
        public bool UntypedNull { get; set; }

        /// <summary>
        /// Timestamps are written as milliseconds since Epoch
        /// </summary>
        public bool TimestampAsMillis { get; set; }

        /// <summary>
        /// Maximum string length before it is wrapped. Negative values denote infinity.
        /// </summary>
        public int LongStringThreshold { get; set; } = -1;

        /// <summary>
        /// Do we write the Ion version marker
        /// </summary>
        public bool WriteVersionMarker { get; set; }
    }
}
