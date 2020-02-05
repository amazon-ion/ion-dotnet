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
using System.IO;
using System.Text;
using Amazon.IonDotnet.Internals;

namespace Amazon.IonDotnet.Tree.Impl
{
    /// <inheritdoc />
    /// <summary>
    /// An Ion clob value.
    /// </summary>
    internal sealed class IonClob : IonLob, IIonClob
    {
        public IonClob(ReadOnlySpan<byte> bytes) : base(bytes)
        {
        }

        private IonClob()
        {
        }

        /// <summary>
        /// Construct a new null.clob value.
        /// </summary>
        public static IonClob NewNull() => new IonClob();

        public override bool IsEquivalentTo(IIonValue other)
        {
            if (!base.IsEquivalentTo(other))
                return false;
            
            if (!(other is IonClob otherClob))
                return false;
            if (NullFlagOn())
                return otherClob.IsNull;
            return !otherClob.IsNull && otherClob.Bytes().SequenceEqual(Bytes());
        }

        internal override void WriteBodyTo(IPrivateWriter writer)
        {
            if (NullFlagOn())
            {
                writer.WriteNull(IonType.Clob);
                return;
            }

            writer.WriteClob(Bytes());
        }

        public override IonType Type() => IonType.Clob;

        /// <summary>
        /// Returns a new <see cref="StreamReader"/> to read the content of this clob.
        /// </summary>
        /// <param name="encoding">Encoding to use.</param>
        public override StreamReader NewReader(Encoding encoding)
        {
            ThrowIfNull();
            return new StreamReader(new MemoryStream(ByteBuffer), encoding);
        }
    }
}
