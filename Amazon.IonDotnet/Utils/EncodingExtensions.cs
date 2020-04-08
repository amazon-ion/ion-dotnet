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
    using System;
    using System.Text;

    internal static class EncodingExtensions
    {
        public static string GetString(this Encoding encoding, ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length == 0)
            {
                return string.Empty;
            }

            unsafe
            {
                fixed (byte* ptr = bytes)
                {
                    return encoding.GetString(ptr, bytes.Length);
                }
            }
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
