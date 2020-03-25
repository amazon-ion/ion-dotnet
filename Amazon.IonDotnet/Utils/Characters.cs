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

namespace Amazon.IonDotnet.Utils
{
    using System.Runtime.CompilerServices;

    /// <summary>
    /// This class support converting between UTF-8 encoding and C# UTF-16 char representation.
    /// </summary>
    internal static class Characters
    {
        private const int UnicodeTwoByteHeader = 0xC0; // 8 + 4 = 12 = 0xC0
        private const int UnicodeThreeByteHeader = 0xE0; // 8+4+2 = 14 = 0xE0
        private const int UnicodeFourByteHeader = 0xF0; // 8+4+2+1 = 15 = 0xF0
        private const int UnicodeTwoByteMask = 0x1F; // 8-3 = 5 bits
        private const int UnicodeThreeByteMask = 0x0F; // 4 bits
        private const int UnicodeFourByteMask = 0x07; // 3 bits
        private const int UnicodeContinuationByteMask = 0x3F; // 6 bits in each continuation

        // Currently unused Ion constants
#pragma warning disable IDE0051 // Remove unused private members
        private const int UnicodeContinuationByteHeader = 0x80;
        private const int MaximumUtf16OneCharCodePoint = 0x0000FFFF;
        private const int SurrogateMask = unchecked((int)0xFFFFFC00);
        private const int SurrogateOffset = 0x00010000;
        private const int HighSurrogate = 0x0000D800; // 0b 1101 1000 0000 0000
        private const int LowSurrogate = 0x0000DC00; // 0b 1101 1100 0000 0000
        private const int SurrogateValueMask = (int)~0xFFFFFC00;
        private const int SurrogateUtf32Shift = 10;
        private const int SurrogateUtf32Offset = 0x10000;
#pragma warning restore IDE0051 // Remove unused private members

        /// <summary>
        /// This enum is used to specify the characters prohibited in certain string types.
        /// </summary>
        public enum ProhibitionContext
        {
            /// <summary>
            /// Short string char i.e. double-quoted string.
            /// </summary>
            ShortChar,

            /// <summary>
            /// Long string char i.e. triple-quoted string.
            /// </summary>
            LongChar,

            /// <summary>
            /// No prohibition.
            /// </summary>
            None,
        }

        public static bool IsProhibited(int c, ProhibitionContext context)
        {
            if (context == ProhibitionContext.None)
            {
                return false;
            }

            var isControl = c <= 0x1F && c >= 0x00;
            var isWhiteSpace = c == 0x09 // tab
                               || c == 0x0B // vertical tab
                               || c == 0x0C // form feed
                               || c == 0x20; // space
            if (context == ProhibitionContext.ShortChar)
            {
                return isControl && !isWhiteSpace;
            }

            var isNewLine = c == 0x0A || c == 0x0D;
            return isControl && !isWhiteSpace && !isNewLine;
        }

        /// <summary>
        /// Put these 2 UTF-8 bytes to a C# <see cref="char"/>.
        /// </summary>
        /// <param name="b1">Byte 1.</param>
        /// <param name="b2">Byte 2.</param>
        /// <returns>2-byte char, since this is a 2-byte utf8.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char Utf8TwoByteScalar(int b1, int b2)
        {
            var c = ((b1 & UnicodeTwoByteMask) << 6) | (b2 & UnicodeContinuationByteMask);
            return (char)c;
        }

        /// <summary>
        /// Convert a 3-byte utf-8 code to 2 C# <see cref="char"/>.
        /// </summary>
        /// <param name="b1">Byte 1.</param>
        /// <param name="b2">Byte 2.</param>
        /// <param name="b3">Byte 3.</param>
        /// <returns>An int that is actually 2 C# chars.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Utf8ThreeByteScalar(int b1, int b2, int b3)
        {
            var c = ((b1 & UnicodeThreeByteMask) << 12)
                    | ((b2 & UnicodeContinuationByteMask) << 6)
                    | (b3 & UnicodeContinuationByteMask);
            return c;
        }

        /// <summary>
        /// Convert a 4-byte utf-8 code to 2 C# <see cref="char"/>.
        /// </summary>
        /// <param name="b1">Byte 1.</param>
        /// <param name="b2">Byte 2.</param>
        /// <param name="b3">Byte 3.</param>
        /// <param name="b4">Byte 4.</param>
        /// <returns>An int that is actually 2 C# chars.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Utf8FourByteScalar(int b1, int b2, int b3, int b4)
        {
            var c = ((b1 & UnicodeFourByteMask) << 18)
                    | ((b2 & UnicodeContinuationByteMask) << 12)
                    | ((b3 & UnicodeContinuationByteMask) << 6)
                    | (b4 & UnicodeContinuationByteMask);
            return c;
        }

        /// <summary>
        /// Look at the first byte and tell how many bytes this UTF8 character is.
        /// </summary>
        /// <param name="firstByte">First byte.</param>
        /// <returns>Size of this UTF8 character.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetUtf8LengthFromFirstByte(int firstByte)
        {
            firstByte &= 0xff;
            if (IsOneByteUtf8(firstByte))
            {
                return 1;
            }

            if (IsTwoByteUtf8(firstByte))
            {
                return 2;
            }

            if (IsThreeByteUtf8(firstByte))
            {
                return 3;
            }

            if (IsFourByteUtf8(firstByte))
            {
                return 4;
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is7BitChar(int c) => (c & ~0x7f) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is8BitChar(int c) => (c & ~0xff) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsOneByteUtf8(int b) => (b & 0x80) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsTwoByteUtf8(int b) => (b & ~UnicodeTwoByteMask) == UnicodeTwoByteHeader;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsThreeByteUtf8(int b) => (b & ~UnicodeThreeByteMask) == UnicodeThreeByteHeader;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsFourByteUtf8(int b) => (b & ~UnicodeFourByteMask) == UnicodeFourByteHeader;
    }
}
