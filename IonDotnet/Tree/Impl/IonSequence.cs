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
using System.Collections.Generic;
using System.Diagnostics;
using IonDotnet.Internals;

namespace IonDotnet.Tree.Impl
{
    /// <inheritdoc cref="IonContainer" />
    /// <summary>
    /// A container that is a sequence of values.
    /// </summary>
    internal abstract class IonSequence : IonContainer, IList<IIonValue>, IIonSequence
    {
        private List<IIonValue> _children;

        protected IonSequence(bool isNull) : base(isNull)
        {
            if (!isNull)
            {
                _children = new List<IIonValue>();
            }
        }

        internal override void WriteBodyTo(IPrivateWriter writer)
        {
            if (NullFlagOn())
            {
                writer.WriteNull(Type());
                return;
            }

            Debug.Assert(_children != null);

            var type = Type();

            if (type != IonType.Datagram)
            {
                writer.StepIn(type);
            }

            foreach (var val in _children)
            {
                val.WriteTo(writer);
            }

            if (type != IonType.Datagram)
            {
                writer.StepOut();
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// The number of children of this container.
        /// </summary>
        public override int Count => _children?.Count ?? 0;

        public override int IndexOf(IIonValue item)
        {
            if (NullFlagOn())
                return -1;

            Debug.Assert(_children != null);
            return _children.IndexOf(item);
        }

        public override void Add(IIonValue item)
        {
            ThrowIfLocked();
            ThrowIfNull();
            if (item is null)
                throw new ArgumentNullException(nameof(item));

            _children.Add(item);
        }

        /// <inheritdoc />
        /// <summary>
        /// Remove the item from container.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>True if the item has been removed</returns>
        public override bool Remove(IIonValue item)
        {
            ThrowIfLocked();
            ThrowIfNull();
            if (NullFlagOn())
                return false;

            Debug.Assert(_children?.Contains(item) == true);
            _children.Remove(item);
            return true;
        }

        public virtual void Insert(int index, IIonValue item)
        {
            ThrowIfLocked();
            ThrowIfNull();
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            
            //this will check range
            _children.Insert(index, item);
        }

        public override void RemoveAt(int index)
        {
            ThrowIfNull();
            if (index >= _children.Count)
                throw new IndexOutOfRangeException($"Container has only {_children.Count} children");

            //this will check for lock
            Remove(_children[index]);
        }

        public override IIonValue GetElementAt(int index)
        {
            ThrowIfNull();
            if (index >= _children.Count)
                throw new IndexOutOfRangeException($"Container has only {_children.Count} children");

            //this will check for lock
            return this[index];
        }

        public override bool IsEquivalentTo(IIonValue other)
        {
            if (!base.IsEquivalentTo(other))
                return false;
           

            var otherSeq = (IonSequence) other;
            if (NullFlagOn())
                return otherSeq.IsNull;
            if (otherSeq.IsNull || otherSeq.Count != Count)
                return false;

            for (int i = 0, l = Count; i < l; i++)
            {
                if (!_children[i].IsEquivalentTo(otherSeq._children[i]))
                    return false;
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
            ThrowIfLocked();
            if (NullFlagOn() && _children == null)
            {
                _children = new List<IIonValue>();
            }
            
            NullFlagOn(false);
            _children.Clear();
        }

        public override bool Contains(IIonValue item)
        {
            if (NullFlagOn() || item == null)
                return false;

            return _children.Contains(item);
        }

        public IIonValue this[int index]
        {
            get
            {
                ThrowIfNull();
                return _children[index];
            }
            set
            {
                ThrowIfLocked();
                ThrowIfNull();
                _children[index] = value;
            }
        }

        public override IEnumerator<IIonValue> GetEnumerator()
        {
            ThrowIfNull();
            return _children.GetEnumerator();
        }
    }
}
