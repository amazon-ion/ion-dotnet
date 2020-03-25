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

namespace Amazon.IonDotnet.Internals.Text
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Numeric format allowed by Ion.
    /// </summary>
    internal enum Radix
    {
        /// <summary>
        /// 0b_nnnnn
        /// </summary>
        Binary,

        /// <summary>
        /// Decimal number
        /// </summary>
        Decimal,

        /// <summary>
        /// 0x_nnnnn
        /// </summary>
        Hex,
    }

#pragma warning disable SA1649 // File name should match first type name
    internal static class RadixExtensions
#pragma warning restore SA1649 // File name should match first type name
    {
        private const string MaxIntDecImage = @"2147483647";
        private const string MinIntDecImage = @"-2147483648";
        private const string MaxLongDecImage = @"9223372036854775807";
        private const string MinLongDecImage = @"-9223372036854775808";

        private const string MaxIntBinaryImage = @"01111111111111111111111111111111";
        private const string MinIntBinaryImage = @"-10000000000000000000000000000000";
        private const string MaxLongBinaryImage = @"0111111111111111111111111111111111111111111111111111111111111111";
        private const string MinLongBinaryImage = @"-1000000000000000000000000000000000000000000000000000000000000000";

        private const string MaxIntHexImage = @"7fffffff";
        private const string MinIntHexImage = @"-80000000";
        private const string MaxLongHexImage = @"7fffffffffffffff";
        private const string MinLongHexImage = @"-8000000000000000";

        /// <summary>Check if the prefix is valid for this radix type.</summary>
        /// <param name="radix">Radix type.</param>
        /// <param name="c">Char prefix.</param>
        /// <returns>True if this prefix valid for this radix type.</returns>
        public static bool IsPrefix(this Radix radix, int c)
        {
            switch (radix)
            {
                case Radix.Binary:
                    return c == 'b' || c == 'B';
                case Radix.Decimal:
                    return false;
                case Radix.Hex:
                    return c == 'x' || c == 'X';
                default:
                    throw new ArgumentOutOfRangeException(nameof(radix), radix, null);
            }
        }

        /// <summary>Check if the char is a valid digit.</summary>
        /// <param name="radix">Radix type.</param>
        /// <param name="c">Char digit.</param>
        /// <returns>True if the char a valid digit.</returns>
        public static bool IsValidDigit(this Radix radix, int c)
        {
            switch (radix)
            {
                case Radix.Binary:
                    return c == '0' || c == '1';
                case Radix.Decimal:
                    return char.IsDigit((char)c);
                case Radix.Hex:
                    return (c >= '0' && c <= '9')
                           || (c >= 'a' && c <= 'f')
                           || (c >= 'A' && c <= 'F');
                default:
                    throw new ArgumentOutOfRangeException(nameof(radix), radix, null);
            }
        }

        public static char NormalizeDigit(this Radix radix, int c)
        {
            switch (radix)
            {
                case Radix.Binary:
                    return (char)c;
                case Radix.Decimal:
                    return (char)c;
                case Radix.Hex:
                    return char.ToLower((char)c);
                default:
                    throw new ArgumentOutOfRangeException(nameof(radix), radix, null);
            }
        }

        /// <summary>Check if the value fits in a 32-bit int.</summary>
        /// <param name="radix">Radix type.</param>
        /// <param name="value">Text representation.</param>
        /// <returns>True if the value fits in a 32-bit int.</returns>
        public static bool IsInt(this Radix radix, in ReadOnlySpan<char> value)
        {
            switch (radix)
            {
                case Radix.Binary:
                    return IsValueWithinBound(value, MaxIntBinaryImage, MinIntBinaryImage);
                case Radix.Decimal:
                    return IsValueWithinBound(value, MaxIntDecImage, MinIntDecImage);
                case Radix.Hex:
                    return IsValueWithinBound(value, MaxIntHexImage, MinIntHexImage);
                default:
                    throw new ArgumentOutOfRangeException(nameof(radix), radix, null);
            }
        }

        /// <summary>Chek if the value fits in a 64-bit long.</summary>
        /// <param name="radix">Radix type.</param>
        /// <param name="value">Text representation.</param>
        /// <returns>True if the value fits in a 64-bit long.</returns>
        public static bool IsLong(this Radix radix, in ReadOnlySpan<char> value)
        {
            switch (radix)
            {
                case Radix.Binary:
                    return IsValueWithinBound(value, MaxLongBinaryImage, MinLongBinaryImage);
                case Radix.Decimal:
                    return IsValueWithinBound(value, MaxLongDecImage, MinLongDecImage);
                case Radix.Hex:
                    return IsValueWithinBound(value, MaxLongHexImage, MinLongHexImage);
                default:
                    throw new ArgumentOutOfRangeException(nameof(radix), radix, null);
            }
        }

        private static bool IsValueWithinBound(in ReadOnlySpan<char> value, string maxImage, string minImage)
        {
            var image = value[0] == '-' ? minImage : maxImage;
            if (value.Length > image.Length)
            {
                return false;
            }

            if (value.Length < image.Length)
            {
                return true;
            }

            return SmallerOrEqualMagnitude(value, image);
        }

        private static bool SmallerOrEqualMagnitude(in ReadOnlySpan<char> value, string image)
        {
            Debug.Assert(value.Length == image.Length, "value.Length does not match image.Length");
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] == image[i])
                {
                    continue;
                }

                return value[i] < image[i];
            }

            return true;
        }
    }
}
