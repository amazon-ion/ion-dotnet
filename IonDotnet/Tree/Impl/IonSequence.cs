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
            //? Remove
            //if (item.Container != null)
            //    throw new ContainedValueException(item);

            _children.Add(item);
            //item.Container = this; //?
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
            //?if (NullFlagOn() || item?.Container != this)
            //    return false;

            Debug.Assert(_children?.Contains(item) == true);
            _children.Remove(item);
            //item.Container = null; //?
            return true;
        }

        public virtual void Insert(int index, IIonValue item)
        {
            ThrowIfLocked();
            ThrowIfNull();
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            //? Remove
            //if (item.Container != null)
            //    throw new ContainedValueException(item);

            //this will check range
            _children.Insert(index, item);
            //item.Container = this; //?
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

            //foreach (var child in _children) //?
            //{
            //    child.Container = null;
            //}

            NullFlagOn(false);
            _children.Clear();
        }

        public override bool Contains(IIonValue item)
        {
            //? _children.Contains(item);
            if (NullFlagOn() || item == null)
                return false;

            return _children.Contains(item);
            //return item.Container == this;
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
