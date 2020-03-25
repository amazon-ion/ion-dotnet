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

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name
#if NETSTANDARD2_0 || NET45
namespace System.Collections.Generic
{
    internal static class DictionaryExtensions
    {
        public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (!dict.ContainsKey(key))
            {
                dict.Add(key, value);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}

#if NET45
namespace System
{
    public static class DateTimeOffsetExtensions
    {
        private const int DaysTo1970 = 719162;
        private const long UnixEpochTicks = TimeSpan.TicksPerDay * DaysTo1970;
        private const long UnixEpochMilliseconds = UnixEpochTicks / TimeSpan.TicksPerMillisecond;

        public static long ToUnixTimeMilliseconds(this DateTimeOffset dto)
        {
            // Truncate sub-millisecond precision before offsetting by the Unix Epoch to avoid
            // the last digit being off by one for dates that result in negative Unix times
            long milliseconds = dto.UtcDateTime.Ticks / TimeSpan.TicksPerMillisecond;
            return milliseconds - UnixEpochMilliseconds;
        }
    }
}
#endif

namespace System.IO
{
    internal static class StreamExtensions
    {
        public static int Read(this Stream stream, Span<byte> dest)
        {
            var buf = new byte[dest.Length];
            var r = stream.Read(buf, 0, buf.Length);
            buf.AsSpan(0, r).CopyTo(dest);
            return r;
        }

        public static void Write(this Stream stream, ReadOnlySpan<byte> src)
        {
            stream.Write(src.ToArray(), 0, src.Length);
        }

        public static Threading.Tasks.Task WriteAsync(this Stream stream, Memory<byte> src)
        {
            return stream.WriteAsync(src.ToArray(), 0, src.Length);
        }
    }
}
#endif

#if NETSTANDARD2_0 || NET45
namespace System.Text
{
    internal static class EncodingExtensions
    {
        public static string GetString(this Encoding encoding, ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length == 0)
            {
                return string.Empty;
            }
#if NET45
            return encoding.GetString(bytes.ToArray());
#else
            unsafe
            {
                fixed (byte* ptr = bytes)
                {
                    return encoding.GetString(ptr, bytes.Length);
                }
            }
#endif
        }

        public static int GetBytes(this Encoding encoding, ReadOnlySpan<char> chars, Span<byte> bytes)
        {
            if (chars.Length == 0)
            {
                return 0;
            }

            unsafe
            {
                fixed (char* sptr = chars)
                fixed (byte* dptr = bytes)
                {
                    return encoding.GetBytes(sptr, chars.Length, dptr, bytes.Length);
                }
            }
        }

        public static int GetByteCount(this Encoding encoding, ReadOnlySpan<char> chars)
        {
            if (chars.Length == 0)
            {
                return 0;
            }

            unsafe
            {
                fixed (char* sptr = chars)
                {
                    return encoding.GetByteCount(sptr, chars.Length);
                }
            }
        }
    }
}
#endif

#if NETSTANDARD2_0 || NET45
namespace System
{
    using System.Runtime.CompilerServices;

    internal static class BitConverterEx
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int SingleToInt32Bits(float value)
        {
            return *((int*)&value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe float Int32BitsToSingle(int value)
        {
            return *((float*)&value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe long DoubleToInt64Bits(double value)
        {
            return *((long*)&value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe double Int64BitsToDouble(long value)
        {
            return *((double*)&value);
        }
    }
}
#endif

namespace System.Threading.Tasks
{
    public static class TaskEx
    {
#if NET45
        public static Task CompletedTask { get; } = Task.FromResult(false);
#else
        public static Task CompletedTask => Task.CompletedTask;
#endif
    }
}
#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore SA1403 // File may only contain a single namespace
#pragma warning restore SA1402 // File may only contain a single type