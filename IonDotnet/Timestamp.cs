using System;
using System.Diagnostics;

namespace IonDotnet
{
    public readonly struct Timestamp
    {
        /// <summary>
        /// Date time value
        /// </summary>
        public readonly DateTime DateTime;

        /// <summary>
        /// Local offset from UTC in minutes
        /// </summary>
        public readonly int LocalOffset;

        public Timestamp(int year, int month, int day, int hour, int minute, int second, decimal frac)
        {
            throw new NotImplementedException();
        }

        public DateTimeOffset AsDateTimeOffset()
        {
            switch (DateTime.Kind)
            {
                case DateTimeKind.Local:
                    return new DateTimeOffset(DateTime, TimeSpan.FromMinutes(LocalOffset));
                case DateTimeKind.Unspecified:
                    throw new InvalidOperationException("Offset is unknown");
                case DateTimeKind.Utc:
                    Debug.Assert(LocalOffset == 0);
                    return new DateTimeOffset(DateTime, TimeSpan.FromMinutes(0));
            }

            return new DateTimeOffset(DateTime, TimeSpan.FromMinutes(LocalOffset));
        }
    }
}
