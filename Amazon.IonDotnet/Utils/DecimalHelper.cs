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
    using System.Diagnostics;

    internal static class DecimalHelper
    {
        /// <summary>
        /// Copy the byte representation of a decimal to the buffer in big-endian byte order (most significant byte first).
        /// </summary>
        /// <param name="bytes">Byte buffer.</param>
        /// <param name="value">Decimal value.</param>
        /// <returns>The end index of the buffer segment that contains meaningful data.</returns>
        public static unsafe int CopyDecimalBigEndian(Span<byte> bytes, decimal value)
        {
            Debug.Assert(bytes.Length >= sizeof(decimal), "bytes.Length is less than sizeof(decimal)");

            var p = (byte*)&value;

            // keep the flag the same
            bytes[0] = p[0];
            bytes[1] = p[1];
            bytes[2] = p[2];
            bytes[3] = p[3];

            // high
            var i = 7;
            while (i > 3 && p[i] == 0)
            {
                i--;
            }

            var hasHigh = i > 3;
            var j = 3;
            while (i > 3)
            {
                bytes[++j] = p[i--];
            }

            // mid
            i = 15;
            bool hasMid;
            if (!hasHigh)
            {
                while (i > 11 && p[i] == 0)
                {
                    i--;
                }

                hasMid = i > 11;
            }
            else
            {
                hasMid = true;
            }

            while (i > 11)
            {
                bytes[++j] = p[i--];
            }

            // lo
            i = 11;
            if (!hasMid)
            {
                while (i > 7 && p[i] == 0)
                {
                    i--;
                }
            }

            while (i > 7)
            {
                bytes[++j] = p[i--];
            }

            return j;
        }
    }
}
