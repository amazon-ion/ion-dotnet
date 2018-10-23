using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace IonDotnet
{
    /// <inheritdoc cref="IEquatable{T}" />
    /// <summary>
    /// This structure represents a timestamp value.
    /// </summary>
    public readonly struct Timestamp : IEquatable<Timestamp>
    {
        public enum Precision : byte
        {
            Year = 1,
            Month = 2,
            Day = 3,
            Minute = 4,
            Second = 5
        }

        private static readonly DateTime EpochLocal = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

        /// <summary>
        /// Date time value
        /// </summary>
        public readonly DateTime DateTimeValue;

        /// <summary>
        /// Local offset from UTC in minutes.
        /// </summary>
        public readonly int LocalOffset;

        /// <summary>
        /// The precision of this value.
        /// </summary>
        public readonly Precision TimestampPrecision;

        /// <summary>
        /// Initialize a new Timestamp structure
        /// </summary>
        public Timestamp(int year, int month, int day, int hour, int minute, int second,
            in decimal frac, Precision precision = Precision.Second)
        {
            TimestampPrecision = precision;

            //offset unknown
            if (frac >= 1)
                throw new ArgumentException("Fraction must be < 1", nameof(frac));

            var ticks = (int) (frac * TimeSpan.TicksPerSecond);
            DateTimeValue = new DateTime(year, month > 0 ? month : 1, day > 0 ? day : 1, hour, minute, second, DateTimeKind.Unspecified)
                .Add(TimeSpan.FromTicks(ticks));
            LocalOffset = 0;
        }

        public Timestamp(int year, int month, int day, int hour, int minute, int second,
            int offset, in decimal frac, Precision precision = Precision.Second)
        {
            TimestampPrecision = precision;

            //offset known
            if (frac >= 1)
                throw new ArgumentException($"Fraction must be < 1: {frac}", nameof(frac));

            var ticks = (int) (frac * TimeSpan.TicksPerSecond);
            var kind = DateTimeKind.Unspecified;
            //offset only makes sense if precision >= Minute
            if (precision < Precision.Minute)
            {
                offset = 0;
            }
            else
            {
                kind = offset == 0 ? DateTimeKind.Utc : DateTimeKind.Local;
            }

            DateTimeValue = new DateTime(year, month > 0 ? month : 1, day > 0 ? day : 1, hour, minute, second, kind)
                .Add(TimeSpan.FromTicks(ticks));
            LocalOffset = offset;
        }

        public Timestamp(int year, int month, int day, int hour, int minute, int second,
            int offset, Precision precision = Precision.Second)
        {
            TimestampPrecision = precision;

            var kind = DateTimeKind.Unspecified;
            //offset only makes sense if precision >= Minute
            if (precision < Precision.Minute)
            {
                offset = 0;
            }
            else
            {
                kind = offset == 0 ? DateTimeKind.Utc : DateTimeKind.Local;
            }

            DateTimeValue = new DateTime(year, month > 0 ? month : 1, day > 0 ? day : 1, hour, minute, second, kind);
            LocalOffset = offset;
        }

        public Timestamp(int year, int month, int day, int hour, int minute, int second,
            Precision precision = Precision.Second)
        {
            TimestampPrecision = precision;

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

        /// <summary>
        /// Initialize the timestamp with a local DateTime value, UTC offset is set to unknown
        /// </summary>
        /// <param name="dateTimeValue">Local datetime value</param>
        public Timestamp(DateTime dateTimeValue)
        {
            TimestampPrecision = Precision.Second;

            DateTimeValue = DateTime.SpecifyKind(dateTimeValue, DateTimeKind.Unspecified);
            //we have no idea about the local offset except when it's 0, so no change here
            LocalOffset = 0;
        }

        /// <summary>
        /// Initialize the timestamp with a <see cref="DateTimeOffset"/> value
        /// </summary>
        /// <param name="dateTimeOffset"></param>
        public Timestamp(DateTimeOffset dateTimeOffset)
        {
            TimestampPrecision = Precision.Second;
            LocalOffset = (int) dateTimeOffset.Offset.TotalMinutes;
            DateTimeValue = DateTime.SpecifyKind(dateTimeOffset.DateTime, LocalOffset == 0
                ? DateTimeKind.Utc
                : DateTimeKind.Local);
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
            if (s.Length < 5)
                throw new FormatException();

            if (!IntTryParseSubString(s, 0, 4, true, out var year))
            {
                throw new FormatException(s);
            }

            if (s[4] == 'T')
            {
                return new Timestamp(year, 1, 1, 0, 0, 0, Precision.Year);
            }

            if (s[4] != '-' || s.Length < 8)
            {
                throw new FormatException(s);
            }

            if (!IntTryParseSubString(s, 5, 2, true, out var month))
            {
                throw new FormatException(s);
            }

            if (s[7] == 'T')
            {
                return new Timestamp(year, month, 1, 0, 0, 0, Precision.Month);
            }

            if (s[7] != '-' || s.Length < 10)
            {
                throw new FormatException(s);
            }

            if (!IntTryParseSubString(s, 8, 2, true, out var day))
            {
                throw new FormatException(s);
            }

            if (s.Length >= 11 && s[10] != 'T')
            {
                throw new FormatException(s);
            }

            if (s.Length <= 11)
            {
                return new Timestamp(year, month, day, 0, 0, 0, Precision.Day);
            }

            //must have hour and minute now
            if (s.Length < 17 || s[13] != ':')
            {
                throw new FormatException(s);
            }

            if (!IntTryParseSubString(s, 11, 2, false, out var hour) || !IntTryParseSubString(s, 14, 2, false, out var minute))
            {
                throw new FormatException(s);
            }

            if (s.Length == 17)
            {
                if (s[16] != 'Z')
                {
                    throw new FormatException(s);
                }

//                return new Timestamp(new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc));
                return new Timestamp(year, month, day, hour, minute, 0, 0, Precision.Minute);
            }

            int offset;
            switch (s[16])
            {
                case '+':
                    offset = GetOffsetMinutes(s, 17);
                    return new Timestamp(year, month, day, hour, minute, 0, offset, Precision.Minute);
                case '-':
                    offset = GetOffsetMinutes(s, 17);
                    if (offset == 0)
                    {
                        //unknown
                        return new Timestamp(year, month, day, hour, minute, 0, Precision.Minute);
                    }

                    return new Timestamp(year, month, day, hour, minute, 0, -offset, Precision.Minute);
            }

            if (s[16] != ':')
            {
                throw new FormatException(s);
            }

            if (s.Length < 19 || !IntTryParseSubString(s, 17, 2, false, out var second))
            {
                throw new FormatException(s);
            }

            if (s.Length == 19)
            {
                return new Timestamp(year, month, day, hour, minute, second);
            }

            switch (s[19])
            {
                case 'Z':
                    return new Timestamp(year, month, day, hour, minute, second, 0);
                case '+':
                    offset = GetOffsetMinutes(s, 20);
                    return new Timestamp(year, month, day, hour, minute, second, offset);
                case '-':
                    offset = GetOffsetMinutes(s, 20);
                    if (offset == 0)
                    {
                        //unknown offset
                        return new Timestamp(year, month, day, hour, minute, second);
                    }

                    return new Timestamp(year, month, day, hour, minute, second, -offset);
                case '.':
                    break;
                default:
                    throw new FormatException(s);
            }

            var fracLength = 0;
            for (var i = 20; i < s.Length; i++)
            {
                if (s[i] == 'Z' || s[i] == '-' || s[i] == '+')
                {
                    break;
                }

                fracLength++;
            }

            if (fracLength == 0 || !DecimalTryParseSubString(s, 19, fracLength + 1, out var frac))
            {
                //this cover the case where s.Length<21
                throw new FormatException(s);
            }

            var idxNext = 20 + fracLength;
            if (idxNext >= s.Length)
            {
                return new Timestamp(year, month, day, hour, minute, second, frac);
            }

            switch (s[idxNext])
            {
                default:
                    throw new FormatException(s);
                case 'Z':
                    return new Timestamp(year, month, day, hour, minute, second, 0, frac);
                case '+':
                    offset = GetOffsetMinutes(s, idxNext + 1);
                    return new Timestamp(year, month, day, hour, minute, second, offset, frac);
                case '-':
                    offset = GetOffsetMinutes(s, idxNext + 1);
                    if (offset == 0)
                    {
                        //unknown offset
                        return new Timestamp(year, month, day, hour, minute, second, frac);
                    }

                    return new Timestamp(year, month, day, hour, minute, second, -offset, frac);
            }
        }

        private static int GetOffsetMinutes(string s, int startIdx)
        {
            if (startIdx + 5 > s.Length || s[startIdx + 2] != ':')
            {
                throw new FormatException();
            }

            if (!IntTryParseSubString(s, startIdx, 2, false, out var hour) || !IntTryParseSubString(s, startIdx + 3, 2, false, out var minute))
            {
                throw new FormatException(s);
            }

            return hour * 60 + minute;
        }

        /// <summary>
        /// Parse a substring to decimal
        /// </summary>
        private static bool DecimalTryParseSubString(string s, int offset, int length, out decimal output)
        {
            if (offset + length > s.Length)
            {
                output = 0;
                return false;
            }
#if NETCOREAPP2_1
            return decimal.TryParse(s.AsSpan().Slice(offset, length), out output);
#else
            return decimal.TryParse(s.Substring(offset, length), out output);
#endif
        }

        /// <summary>
        /// Parse a substring to integer
        /// </summary>
        private static bool IntTryParseSubString(string s, int offset, int length, bool largerThanZero, out int output)
        {
            if (offset + length > s.Length)
            {
                output = 0;
                return false;
            }
#if NETCOREAPP2_1
            return int.TryParse(s.AsSpan().Slice(offset, length), out output) && (!largerThanZero || output > 0);
#else
            output = 0;
            for (var i = 0; i < length; i++)
            {
                if (!char.IsDigit(s[offset]))
                    return false;
                output = output * 10 + s[offset] - '0';
                offset++;
            }

            return !largerThanZero || output > 0;
#endif
        }

        public override string ToString()
        {
            return DateTimeValue.Kind == DateTimeKind.Unspecified
                ? DateTimeValue.ToString("O")
                : AsDateTimeOffset().ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        }

        public bool OffsetKnown => DateTimeValue.Kind != DateTimeKind.Unspecified;

        private long Ticks
        {
            get
            {
                var ticks = DateTimeValue.Ticks;
                if (DateTimeValue.Kind == DateTimeKind.Local)
                {
                    ticks -= TimeSpan.FromMinutes(LocalOffset).Ticks;
                }

                return ticks;
            }
        }

        //override stuffs

        public static bool operator ==(Timestamp x, Timestamp y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(Timestamp x, Timestamp y) => !(x == y);

        public bool Equals(Timestamp other)
        {
            if (TimestampPrecision != other.TimestampPrecision)
                return false;
            if (DateTimeValue.Kind == DateTimeKind.Unspecified)
            {
                //unknown offset
                return other.DateTimeValue.Kind == DateTimeKind.Unspecified && other.DateTimeValue == DateTimeValue;
            }

            if (other.DateTimeValue.Kind == DateTimeKind.Unspecified)
                return false;

            //both must now be convertible to ticks
            return Ticks == other.Ticks;
        }


        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Timestamp other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (DateTimeValue.GetHashCode() * 397) ^ LocalOffset;
            }
        }
    }
}
