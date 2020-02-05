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

using System;
using System.Diagnostics;

namespace Amazon.IonDotnet.Tree.Impl
{
    /// <inheritdoc />
    /// <summary>
    /// A value that holds a blob of byte data.
    /// </summary>
    internal abstract class IonLob : IonValue, IIonLob
    {
        protected byte[] ByteBuffer;

        protected IonLob() : base(true)
        {
        }

        protected IonLob(ReadOnlySpan<byte> bytes) : base(false)
        {
            SetBytes(bytes);
        }

        /// <summary>
        /// Get the view of the bytes in this blob.
        /// </summary>
        /// <returns>Read-only view of the bytes in this lob.</returns>
        /// <exception cref="NullValueException">If this lob is null.</exception>
        public override ReadOnlySpan<byte> Bytes()
        {
            ThrowIfNull();
            Debug.Assert(ByteBuffer != null);
            return ByteBuffer.AsSpan();
        }

        /// <summary>
        /// Copy the bytes from the buffer to this lob.
        /// </summary>
        /// <param name="buffer">Byte buffer</param>
        public override void SetBytes(ReadOnlySpan<byte> buffer)
        {
            ThrowIfLocked();
            //this is bad but this operation is pretty non-common.
            Array.Resize(ref ByteBuffer, buffer.Length);
            buffer.CopyTo(ByteBuffer);
            NullFlagOn(false);
        }

        public override void MakeNull()
        {
            base.MakeNull();
            ByteBuffer = null;
        }

        public override int ByteSize()
        {
            return ByteBuffer.Length;
        }
    }
}
