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
using System.Collections;
using System.Collections.Generic;

namespace Amazon.IonDotnet.Tree.Impl
{
    /// <inheritdoc cref="IonValue" />
    /// <summary>
    /// Base class for all container type (List,Struct,Sexp,Datagram) Ion values.
    /// This class also implements the <see cref="ICollection"/> interface.
    /// </summary>
    internal abstract class IonContainer : IonValue, ICollection<IIonValue>, IIonContainer
    {
        protected IonContainer(bool isNull) : base(isNull)
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Add an Ion value to the container.
        /// </summary>
        /// <param name="item">Ion value.</param>
        public override abstract void Add(IIonValue item);

        /// <inheritdoc />
        /// <summary>
        /// Remove the item from container.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>True if the item has been removed</returns>
        public override abstract bool Remove(IIonValue item);


        /// <inheritdoc />
        /// <summary>
        /// The number of children of this container.
        /// </summary>
        public override abstract int Count { get; }


        /// <inheritdoc />
        /// <summary>
        /// Clear the content of this container.
        /// </summary>
        /// <remarks>
        /// If this container is null, it will be set to a non-null empty container.
        /// </remarks>
        public override abstract void Clear();

        /// <inheritdoc />
        /// <summary>
        /// Returns true if the container contains an Ion value.
        /// </summary>
        /// <param name="item">Ion value.</param>
        public override abstract bool Contains(IIonValue item);

        /// <inheritdoc />
        /// <summary>
        /// This operation is not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">This operation is not supported.</exception>
        public override void CopyTo(IIonValue[] array, int arrayIndex) => throw new NotSupportedException();

        public override void MakeNull()
        {
            Clear();
            base.MakeNull();
        }

        public override abstract IEnumerator<IIonValue> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
