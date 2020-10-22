/*
 * Copyright 2020 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

namespace Amazon.IonDotnet
{
    using System;
    using System.Diagnostics;
    using System.Globalization;

    /// <inheritdoc cref="IEquatable{T}" />
    /// <summary>
    /// This structure represents a timestamp value.
    /// </summary>
    public readonly struct Timestamp : IEquatable<Timestamp>
    {
        /// <summary>
        /// Date time value.
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
        /// Fractional seconds (milliseconds).
        /// </summary>
        public readonly decimal FractionalSecond;

        private static readonly DateTime EpochLocal = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

        /// <summary>
        /// Initializes a new instance of the <see cref="Timestamp"/> struct.
        /// </summary>
        /// <param name="year">The year.</param>
        /// <param name="month">The month.</param>
        /// <param name="day">The day.</param>
        /// <param name="hour">The hour.</param>
        /// <param name="minute">The minute.</param>
        /// <param name="second">The second.</param>
        /// <param name="frac">The fractional seconds.</param>
        /// <param name="precision">The precision of the Timestamp.</param>
        public Timestamp(
            int year,
            int month,
            int day,
            int hour,
            int minute,
            int second,
            in decimal frac,
            Precision precision = Precision.Second)
        {
            this.TimestampPrecision = precision;

            // offset unknown
            if (frac >= 1)
            {
                throw new ArgumentException("Fraction must be < 1", nameof(frac));
            }

            this.FractionalSecond = frac;
            this.DateTimeValue = new DateTime(year, month > 0 ? month : 1, day > 0 ? day : 1, hour, minute, second, DateTimeKind.Unspecified)
                .AddSeconds(decimal.ToDouble(frac));
            this.LocalOffset = 0;
        }

        public Timestamp(
            int year,
            int month,
            int day,
            int hour,
            int minute,
            int second,
            Precision precision = Precision.Second)
        {
            this.TimestampPrecision = precision;
            this.FractionalSecond = 0;

            // offset known
            this.DateTimeValue = new DateTime(year, month > 0 ? month : 1, day > 0 ? day : 1, hour, minute, second, DateTimeKind.Unspecified);
            this.LocalOffset = 0;
        }

        public Timestamp(
            int year,
            int month,
            int day,
            int hour,
            int minute,
            int second,
            int offset,
            Precision precision = Precision.Second)
            : this(year, month, day, hour, minute, second, offset, 0, precision)
        {
        }

        public Timestamp(
            int year,
            int month,
            int day,
            int hour,
            int minute,
            int second,
            int offset,
            in decimal frac,
            Precision precision = Precision.Second)
        {
            this.TimestampPrecision = precision;

            // offset known
            if (frac >= 1)
            {
                throw new ArgumentException($"Fraction must be < 1: {frac}", nameof(frac));
            }

            this.FractionalSecond = frac;
            var kind = DateTimeKind.Unspecified;

            // offset only makes sense if precision >= Minute
            if (precision < Precision.Minute)
            {
                offset = 0;
            }
            else
            {
                kind = offset == 0 ? DateTimeKind.Utc : DateTimeKind.Local;
            }

            const int maxOffset = 14 * 60;
            var shift = TimeSpan.FromSeconds(decimal.ToDouble(frac));
            if (offset > maxOffset || offset < -maxOffset)
            {
                var minuteShift = (offset / maxOffset) * maxOffset;
                offset %= maxOffset;
                shift -= TimeSpan.FromMinutes(minuteShift);
            }

            this.DateTimeValue = new DateTime(year, month > 0 ? month : 1, day > 0 ? day : 1, hour, minute, second, kind)
                .Add(shift);

            this.LocalOffset = offset;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Timestamp"/> struct,
        /// with a local DateTime value, UTC offset is set to unknown.
        /// </summary>
        /// <param name="dateTimeValue">Local datetime value.</param>
        public Timestamp(DateTime dateTimeValue)
        {
            this.TimestampPrecision = Precision.Second;
            this.FractionalSecond = 0;

            this.DateTimeValue = DateTime.SpecifyKind(dateTimeValue, DateTimeKind.Unspecified);

            // we have no idea about the local offset except when it's 0, so no change here
            this.LocalOffset = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Timestamp"/> struct,
        /// with a <see cref="DateTimeOffset"/> value.
        /// </summary>
        /// <param name="dateTimeOffset">The DateTimeOffset value.</param>
        public Timestamp(DateTimeOffset dateTimeOffset)
        {
            this.TimestampPrecision = Precision.Second;
            this.FractionalSecond = 0;
            this.LocalOffset = (int)dateTimeOffset.Offset.TotalMinutes;
            this.DateTimeValue = DateTime.SpecifyKind(
                dateTimeOffset.DateTime,
                this.LocalOffset == 0 ? DateTimeKind.Utc : DateTimeKind.Local);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Timestamp"/> struct with different components, offset and fractional second.
        /// Timestamps in the binary encoding are always in UTC, while in the text encoding are in the local time. This means transcoding
        /// requires a conversion between UTC and local time, ergo we add the offset and fractional seconds to this value.
        /// </summary>
        /// <param name="year">Year.</param>
        /// <param name="month">Month.</param>
        /// <param name="day">Day.</param>
        /// <param name="hour">Hour.</param>
        /// <param name="minute">Minute.</param>
        /// <param name="second">Second.</param>
        /// <param name="offset">Offset value.</param>
        /// <param name="frac">Fractional second value.</param>
        /// <param name="precision">The precision of the value.</param>
        /// <param name="kind">DateTimeKind.</param>
        internal Timestamp(
            int year,
            int month,
            int day,
            int hour,
            int minute,
            int second,
            int offset,
            in decimal frac,
            Precision precision,
            DateTimeKind kind)
        {
            this.TimestampPrecision = precision;
            if (frac >= 1)
            {
                throw new ArgumentException("Fraction must be < 1", nameof(frac));
            }

            this.FractionalSecond = frac;
            this.DateTimeValue = new DateTime(year, month > 0 ? month : 1, day > 0 ? day : 1, hour, minute, second, kind)
                .AddMinutes(offset)
                .AddSeconds(decimal.ToDouble(frac));
            this.LocalOffset = offset;
        }

        public enum Precision : byte
        {
            Year = 1,
            Month = 2,
            Day = 3,
            Minute = 4,
            Second = 5,
        }

        /// <summary>
        /// Gets the milliseconds since epoch.
        /// </summary>
        public long Milliseconds
        {
            get
            {
                switch (this.DateTimeValue.Kind)
                {
                    case DateTimeKind.Unspecified:
                        return (long)(this.DateTimeValue - EpochLocal).TotalMilliseconds;
                    case DateTimeKind.Local:
                    case DateTimeKind.Utc:
                        return this.AsDateTimeOffset().ToUnixTimeMilliseconds();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public bool OffsetKnown => this.DateTimeValue.Kind != DateTimeKind.Unspecified;

        private long Ticks
        {
            get
            {
                var ticks = this.DateTimeValue.Ticks;
                if (this.DateTimeValue.Kind == DateTimeKind.Local)
                {
                    ticks -= TimeSpan.FromMinutes(this.LocalOffset).Ticks;
                }

                return ticks;
            }
        }

        public static bool operator ==(Timestamp x, Timestamp y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(Timestamp x, Timestamp y) => !(x == y);

        /// <summary>
        /// Parse an ISO-8601 Datetime format to the Timestamp.
        /// </summary>
        /// <param name="s">ISO-8601 Datetime string.</param>
        /// <returns>Timestamp object.</returns>
        /// <exception cref="FormatException">Parameter is not a correct ISO-8601 string format.</exception>
        public static Timestamp Parse(string s)
        {
            if (s.Length < 5)
            {
                throw new FormatException();
            }

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

            // must have hour and minute now
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
                        // unknown
                        return new Timestamp(year, month, day, hour, minute, 0, Precision.Minute);
                    }

                    return new Timestamp(year, month, day, hour, minute, 0, -offset, Precision.Minute);
            }

            if (s[16] != ':')
            {
                throw new FormatException(s);
            }

            if (s.Length < 20 || !IntTryParseSubString(s, 17, 2, false, out var second))
            {
                throw new FormatException(s);
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
                        // unknown offset
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
                // this cover the case where s.Length < 21
                throw new FormatException(s);
            }

            var idxNext = 20 + fracLength;
            if (idxNext == s.Length)
            {
                // this covers the case where offset is missing after fractional seconds.
                throw new FormatException(s + " requires an offset.");
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
                        // unknown offset
                        return new Timestamp(year, month, day, hour, minute, second, frac);
                    }

                    return new Timestamp(year, month, day, hour, minute, second, -offset, frac);
            }
        }

        public DateTimeOffset AsDateTimeOffset()
        {
            switch (this.DateTimeValue.Kind)
            {
                case DateTimeKind.Local:
                    return new DateTimeOffset(DateTime.SpecifyKind(this.DateTimeValue, DateTimeKind.Unspecified), TimeSpan.FromMinutes(this.LocalOffset));
                case DateTimeKind.Unspecified:
                    throw new InvalidOperationException("Offset is unknown");
                case DateTimeKind.Utc:
                    Debug.Assert(this.LocalOffset == 0, "LocalOffset is not 0");
                    return new DateTimeOffset(this.DateTimeValue, TimeSpan.Zero);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return this.DateTimeValue.Kind == DateTimeKind.Unspecified
                ? this.DateTimeValue.ToString("O") + "-00:00"
                : this.AsDateTimeOffset().ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        }

        public bool Equals(Timestamp other)
        {
            if (this.TimestampPrecision != other.TimestampPrecision)
            {
                return false;
            }

            if (this.DateTimeValue.Kind == DateTimeKind.Unspecified)
            {
                // unknown offset
                return other.DateTimeValue.Kind == DateTimeKind.Unspecified && other.DateTimeValue == this.DateTimeValue;
            }

            if (other.DateTimeValue.Kind == DateTimeKind.Unspecified)
            {
                return false;
            }

            // both must now be convertible to ticks
            return this.Ticks == other.Ticks;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            return obj is Timestamp other && this.Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (this.DateTimeValue.GetHashCode() * 397) ^ this.LocalOffset;
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

            int offset = (hour * 60) + minute;

            if (hour > 23 || minute > 59)
            {
                throw new FormatException($"{s} - Offset must be between -23:59 and 23:59");
            }

            return offset;
        }

        /// <summary>
        /// Parse a substring to decimal.
        /// </summary>
        private static bool DecimalTryParseSubString(string s, int offset, int length, out decimal output)
        {
            if (offset + length > s.Length)
            {
                output = 0;
                return false;
            }

            string decimalText = s.Substring(offset, length);
            return decimal.TryParse(decimalText, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out output);
        }

        /// <summary>
        /// Parse a substring to integer.
        /// </summary>
        private static bool IntTryParseSubString(string s, int offset, int length, bool largerThanZero, out int output)
        {
            if (offset + length > s.Length)
            {
                output = 0;
                return false;
            }

            output = 0;
            for (var i = 0; i < length; i++)
            {
                if (!char.IsDigit(s[offset]))
                {
                    return false;
                }

                output = (output * 10) + s[offset] - '0';
                offset++;
            }

            return !largerThanZero || output > 0;
        }
    }
}
