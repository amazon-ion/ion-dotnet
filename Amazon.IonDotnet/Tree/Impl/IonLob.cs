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

namespace Amazon.IonDotnet.Tree.Impl
{
    using System;
    using System.Diagnostics;

    /// <inheritdoc />
    /// <summary>
    /// A value that holds a blob of byte data.
    /// </summary>
    internal abstract class IonLob : IonValue, IIonLob
    {
        protected byte[] byteBuffer;

        protected IonLob()
            : base(true)
        {
        }

        protected IonLob(ReadOnlySpan<byte> bytes)
            : base(false)
        {
            this.SetBytes(bytes);
        }

        /// <summary>
        /// Get the view of the bytes in this blob.
        /// </summary>
        /// <returns>Read-only view of the bytes in this lob.</returns>
        /// <exception cref="NullValueException">If this lob is null.</exception>
        public override ReadOnlySpan<byte> Bytes()
        {
            this.ThrowIfNull();
            Debug.Assert(this.byteBuffer != null, "byteBuffer is null");
            return this.byteBuffer.AsSpan();
        }

        /// <summary>
        /// Copy the bytes from the buffer to this lob.
        /// </summary>
        /// <param name="buffer">Byte buffer.</param>
        public override void SetBytes(ReadOnlySpan<byte> buffer)
        {
            this.ThrowIfLocked();
            Array.Resize(ref this.byteBuffer, buffer.Length);
            buffer.CopyTo(this.byteBuffer);
            this.NullFlagOn(false);
        }

        public override void MakeNull()
        {
            base.MakeNull();
            this.byteBuffer = null;
        }

        public override int ByteSize()
        {
            return this.byteBuffer.Length;
        }
    }
}
