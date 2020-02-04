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
using IonDotnet.Internals;

namespace IonDotnet.Tree.Impl
{
    /// <inheritdoc />
    /// <summary>
    /// An Ion blob value.
    /// </summary>
    internal sealed class IonBlob : IonLob, IIonBlob
    {
        public IonBlob(ReadOnlySpan<byte> bytes) : base(bytes)
        {
        }

        private IonBlob()
        {
        }

        /// <summary>
        /// Construct a new null.blob value.
        /// </summary>
        public static IonBlob NewNull() => new IonBlob();

        public override bool IsEquivalentTo(IIonValue other)
        {
            if (!base.IsEquivalentTo(other))
                return false;

            var otherBlob = (IonBlob) other;

            if (NullFlagOn())
                return otherBlob.IsNull;
            return !otherBlob.IsNull && otherBlob.Bytes().SequenceEqual(Bytes());
        }

        internal override void WriteBodyTo(IPrivateWriter writer)
        {
            if (NullFlagOn())
            {
                writer.WriteNull(IonType.Blob);
                return;
            }

            writer.WriteBlob(Bytes());
        }

        public override IonType Type() => IonType.Blob;
    }
}
