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
    using System.Collections.Generic;
    using System.Diagnostics;
    using Amazon.IonDotnet.Internals;

    /// <inheritdoc cref="IonContainer" />
    /// <summary>
    /// A container that is a sequence of values.
    /// </summary>
    internal abstract class IonSequence : IonContainer, IList<IIonValue>, IIonSequence
    {
        private List<IIonValue> children;

        protected IonSequence(bool isNull)
            : base(isNull)
        {
            if (!isNull)
            {
                this.children = new List<IIonValue>();
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// The number of children of this container.
        /// </summary>
        public override int Count => this.children?.Count ?? 0;

        public IIonValue this[int index]
        {
            get
            {
                this.ThrowIfNull();
                return this.children[index];
            }

            set
            {
                this.ThrowIfLocked();
                this.ThrowIfNull();
                this.children[index] = value;
            }
        }

        public override int IndexOf(IIonValue item)
        {
            if (this.NullFlagOn())
            {
                return -1;
            }

            Debug.Assert(this.children != null, "children is null");
            return this.children.IndexOf(item);
        }

        public override void Add(IIonValue item)
        {
            this.ThrowIfLocked();
            this.ThrowIfNull();
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            this.children.Add(item);
        }

        /// <inheritdoc />
        /// <summary>
        /// Remove the item from container.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>True if the item has been removed.</returns>
        public override bool Remove(IIonValue item)
        {
            this.ThrowIfLocked();
            this.ThrowIfNull();
            if (this.NullFlagOn())
            {
                return false;
            }

            Debug.Assert(this.children?.Contains(item) == true, "children does not contain item");
            this.children.Remove(item);
            return true;
        }

        public virtual void Insert(int index, IIonValue item)
        {
            this.ThrowIfLocked();
            this.ThrowIfNull();
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            // this will check range
            this.children.Insert(index, item);
        }

        public override void RemoveAt(int index)
        {
            this.ThrowIfNull();
            if (index >= this.children.Count)
            {
                throw new IndexOutOfRangeException($"Container has only {this.children.Count} children");
            }

            // this will check for lock
            this.Remove(this.children[index]);
        }

        public override IIonValue GetElementAt(int index)
        {
            this.ThrowIfNull();
            if (index >= this.children.Count)
            {
                throw new IndexOutOfRangeException($"Container has only {this.children.Count} children");
            }

            // this will check for lock
            return this[index];
        }

        public override bool IsEquivalentTo(IIonValue other)
        {
            if (!base.IsEquivalentTo(other))
            {
                return false;
            }

            var otherSeq = (IonSequence)other;
            if (this.NullFlagOn())
            {
                return otherSeq.IsNull;
            }

            if (otherSeq.IsNull || otherSeq.Count != this.Count)
            {
                return false;
            }

            for (int i = 0, l = this.Count; i < l; i++)
            {
                if (!this.children[i].IsEquivalentTo(otherSeq.children[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc />
        /// <summary>
        /// Clear the content of this container.
        /// </summary>
        /// <remarks>
        /// If this container is null, it will be set to a non-null empty container.
        /// </remarks>
        public override void Clear()
        {
            this.ThrowIfLocked();
            if (this.NullFlagOn() && this.children == null)
            {
                this.children = new List<IIonValue>();
            }

            this.NullFlagOn(false);
            this.children.Clear();
        }

        public override bool Contains(IIonValue item)
        {
            if (this.NullFlagOn() || item == null)
            {
                return false;
            }

            return this.children.Contains(item);
        }

        public override IEnumerator<IIonValue> GetEnumerator()
        {
            this.ThrowIfNull();
            return this.children.GetEnumerator();
        }

        internal override void WriteBodyTo(IPrivateWriter writer)
        {
            if (this.NullFlagOn())
            {
                writer.WriteNull(this.Type());
                return;
            }

            Debug.Assert(this.children != null, "children is null");

            var type = this.Type();

            if (type != IonType.Datagram)
            {
                writer.StepIn(type);
            }

            foreach (var val in this.children)
            {
                val.WriteTo(writer);
            }

            if (type != IonType.Datagram)
            {
                writer.StepOut();
            }
        }
    }
}
