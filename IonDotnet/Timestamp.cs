using System;
using System.Diagnostics;
using System.Numerics;

namespace IonDotnet
{
    public readonly struct Timestamp
    {
        internal static readonly DateTime EpochLocal = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

        /// <summary>
        /// Date time value
        /// </summary>
        public readonly DateTime DateTimeValue;

        /// <summary>
        /// Local offset from UTC in minutes
        /// </summary>
        public readonly int LocalOffset;

        public Timestamp(int year, int month, int day, int hour, int minute, int second, in decimal frac)
        {
            //offset unknown
            if (frac >= 1) throw new ArgumentException("Fraction must be < 1", nameof(frac));

            var ticks = (int) (frac * TimeSpan.TicksPerSecond);
            DateTimeValue = new DateTime(year, month > 0 ? month : 1, day > 0 ? day : 1, hour, minute, second, DateTimeKind.Unspecified)
                .Add(TimeSpan.FromTicks(ticks));
            LocalOffset = 0;
        }

        public Timestamp(int year, int month, int day, int hour, int minute, int second, int offset, in decimal frac)
        {
            //offset known
            if (frac >= 1) throw new ArgumentException("Fraction must be < 1", nameof(frac));

            var ticks = (int) (frac * TimeSpan.TicksPerSecond);
            DateTimeValue = new DateTime(year, month > 0 ? month : 1, day > 0 ? day : 1, hour, minute, second, offset == 0 ? DateTimeKind.Utc : DateTimeKind.Local)
                .Add(TimeSpan.FromTicks(ticks));
            LocalOffset = offset;
        }

        public Timestamp(int year, int month, int day, int hour, int minute, int second, int offset)
        {
            //no frag, no perf lost
            DateTimeValue = new DateTime(year, month > 0 ? month : 1, day > 0 ? day : 1, hour, minute, second,
                offset == 0 ? DateTimeKind.Utc : DateTimeKind.Local);
            LocalOffset = offset;
        }

        public Timestamp(int year, int month, int day, int hour, int minute, int second)
        {
            //no frag, no perf lost
            //offset known
            DateTimeValue = new DateTime(year, month > 0 ? month : 1, day > 0 ? day : 1, hour, minute, second, DateTimeKind.Unspecified);
            LocalOffset = 0;
        }

        public DateTimeOffset AsDateTimeOffset()
        {
            switch (DateTimeValue.Kind)
            {
                case DateTimeKind.Local:
                    var hourOffset = LocalOffset / 60;
                    var minuteOffsetRemainder = LocalOffset % 60;
                    return new DateTimeOffset(DateTime.SpecifyKind(DateTimeValue, DateTimeKind.Unspecified), TimeSpan.FromHours(hourOffset))
                           - TimeSpan.FromMinutes(minuteOffsetRemainder);
                case DateTimeKind.Unspecified:
                    throw new InvalidOperationException("Offset is unknown");
                case DateTimeKind.Utc:
                    Debug.Assert(LocalOffset == 0);
                    return new DateTimeOffset(DateTimeValue, TimeSpan.Zero);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public Timestamp(DateTime dateTimeValue)
        {
            DateTimeValue = dateTimeValue;
            //we have no idea about the local offset except when it's 0, so no change here
            LocalOffset = 0;
        }

        public Timestamp(DateTimeOffset dateTimeOffset)
        {
            LocalOffset = (int) dateTimeOffset.Offset.TotalMinutes;
            DateTimeValue = DateTime.SpecifyKind(dateTimeOffset.DateTime, LocalOffset == 0 ? DateTimeKind.Utc : DateTimeKind.Local);
        }

        /// <summary>
        /// Get the milliseconds since epoch
        /// </summary>
        public long Milliseconds
        {
            get
            {
                switch (DateTimeValue.Kind)
                {
                    case DateTimeKind.Unspecified:
                        return (long) (DateTimeValue - EpochLocal).TotalMilliseconds;
                    case DateTimeKind.Local:
                    case DateTimeKind.Utc:
                        return AsDateTimeOffset().ToUnixTimeMilliseconds();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Parse an ISO-8601 Datetime format to the Timestamp
        /// </summary>
        /// <param name="s">ISO-8601 Datetime string</param>
        /// <returns>Timestamp object</returns>
        /// <exception cref="FormatException">Parameter is not a correct ISO-8601 string format</exception>
        public static Timestamp Parse(string s)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return DateTimeValue.Kind == DateTimeKind.Unspecified
                ? DateTimeValue.ToString("O")
                : AsDateTimeOffset().ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
