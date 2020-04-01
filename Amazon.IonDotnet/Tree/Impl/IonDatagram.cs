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
    using System.Diagnostics;

    /// <inheritdoc />
    /// <summary>
    /// An ion datagram is a special kind of value which represents a stream of Ion values.
    /// </summary>
    internal sealed class IonDatagram : IonSequence, IIonDatagram
    {
        public IonDatagram()
            : base(false)
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Use strict reference equality for datagram.
        /// </summary>
        public override bool IsEquivalentTo(IIonValue other) => other == this;

        public override IonType Type() => IonType.Datagram;

        /// <summary>
        /// Adding an item to the datagram will mark the current symbol table.
        /// </summary>
        /// <param name="item">The IIonValue to add.</param>
        public override void Add(IIonValue item)
        {
            base.Add(item);
            Debug.Assert(item != null, nameof(item) + " != null");
        }

        public int GetHashCode(IonValue obj) => obj.GetHashCode();
    }
}
