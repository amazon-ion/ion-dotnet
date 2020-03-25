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
    using System.IO;

    public static class StreamExtension
    {
        /// <summary>
        /// Attempts to get the data written to a <see cref="MemoryStream"/> as efficiently as possible.
        /// </summary>
        /// <param name="memStream">Memory stream.</param>
        /// <returns>Byte array of the written data.</returns>
        public static byte[] GetWrittenBuffer(this MemoryStream memStream)
        {
            try
            {
                var buffer = memStream.GetBuffer();
                if (buffer.Length == memStream.Length)
                {
                    // this means the buffer is correctly set to the written data by setting MemoryBuffer.Capacity
                    return buffer;
                }

                return memStream.ToArray();
            }
            catch (UnauthorizedAccessException)
            {
                // No access to the underlying buffer
                return memStream.ToArray();
            }
        }

        public static void CheckStreamCanWrite(this Stream outputStream)
        {
            if (!outputStream.CanWrite)
            {
                throw new ArgumentException("Output stream must be writable", nameof(outputStream));
            }
        }
    }
}
