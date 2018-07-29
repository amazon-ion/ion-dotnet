using System;
using System.Diagnostics;
using System.Numerics;

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

        public Timestamp(int year, int month, int day, int hour, int minute, int second, in decimal frac)
        {
            //offset unknown
            if (frac >= 1) throw new ArgumentException("Fraction must be < 1", nameof(frac));

            var ticks = (int) (frac * TimeSpan.TicksPerSecond);
            DateTime = new DateTime(year, month > 0 ? month : 1, day > 0 ? day : 1, hour, minute, second, DateTimeKind.Unspecified)
                .Add(TimeSpan.FromTicks(ticks));
            LocalOffset = 0;
        }

        public Timestamp(int year, int month, int day, int hour, int minute, int second, int offset, in decimal frac)
        {
            //offset known
            if (frac >= 1) throw new ArgumentException("Fraction must be < 1", nameof(frac));

            var ticks = (int) (frac * TimeSpan.TicksPerSecond);
            DateTime = new DateTime(year, month > 0 ? month : 1, day > 0 ? day : 1, hour, minute, second, offset == 0 ? DateTimeKind.Utc : DateTimeKind.Local)
                .Add(TimeSpan.FromTicks(ticks));
            LocalOffset = offset;
        }

        public Timestamp(int year, int month, int day, int hour, int minute, int second, int offset)
        {
            //no frag, no perf lost
            DateTime = new DateTime(year, month > 0 ? month : 1, day > 0 ? day : 1, hour, minute, second,
                offset == 0 ? DateTimeKind.Utc : DateTimeKind.Local);
            LocalOffset = offset;
        }

        public Timestamp(int year, int month, int day, int hour, int minute, int second)
        {
            //no frag, no perf lost
            //offset known
            DateTime = new DateTime(year, month > 0 ? month : 1, day > 0 ? day : 1, hour, minute, second, DateTimeKind.Unspecified);
            LocalOffset = 0;
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

        public Timestamp(DateTime dateTime)
        {
            DateTime = dateTime;
            //we have no idea about the local offset except when it's 0, so no change here
            LocalOffset = 0;
        }

        public Timestamp(DateTimeOffset dateTimeOffset)
        {
            LocalOffset = (int) dateTimeOffset.Offset.TotalMinutes;
            DateTime = DateTime.SpecifyKind(dateTimeOffset.DateTime, LocalOffset == 0 ? DateTimeKind.Utc : DateTimeKind.Local);
        }
    }
}
