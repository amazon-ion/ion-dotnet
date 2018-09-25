using System;
using System.Collections.Generic;
using System.Diagnostics;
using IonDotnet.Internals;

namespace IonDotnet.Tree
{
    public abstract class IonSequence : IonContainer, IList<IonValue>
    {
        protected List<IonValue> _children;

        protected IonSequence(bool isNull) : base(isNull)
        {
            if (!isNull)
            {
                _children = new List<IonValue>();
            }
        }

        internal override void WriteBodyTo(IPrivateWriter writer)
        {
            if (NullFlagOn())
            {
                writer.WriteNull(Type);
                return;
            }

            Debug.Assert(_children != null);

            writer.StepIn(Type);
            foreach (var val in _children)
            {
                val.WriteTo(writer);
            }

            writer.StepOut();
        }

        /// <inheritdoc />
        /// <summary>
        /// The number of children of this container.
        /// </summary>
        public override int Count => _children?.Count ?? 0;

        public int IndexOf(IonValue item)
        {
            if (NullFlagOn())
                return -1;

            Debug.Assert(_children != null);
            return _children.IndexOf(item);
        }

        public override void Add(IonValue item)
        {
            ThrowIfLocked();
            ThrowIfNull();
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (item.Container != null)
                throw new ContainedValueException();

            _children.Add(item);
            item.Container = this;
        }

        /// <inheritdoc />
        /// <summary>
        /// Remove the item from container.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>True if the item has been removed</returns>
        public override bool Remove(IonValue item)
        {
            ThrowIfLocked();
            if (NullFlagOn() || item?.Container != this)
                return false;

            Debug.Assert(_children?.Contains(item) == true);
            _children.Remove(item);
            item.Container = null;
            return true;
        }

        public virtual void Insert(int index, IonValue item)
        {
            ThrowIfLocked();
            ThrowIfNull();
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (item.Container != null)
                throw new ContainedValueException();

            //this will check range
            _children.Insert(index, item);
            item.Container = this;
        }

        public void RemoveAt(int index)
        {
            ThrowIfNull();
            if (index >= _children.Count)
                throw new IndexOutOfRangeException($"Container has only {_children.Count} children");

            //this will check for lock
            Remove(_children[index]);
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
                _children = new List<IonValue>();
            }

            foreach (var child in _children)
            {
                child.Container = null;
            }

            NullFlagOn(false);
            _children.Clear();
        }

        public override bool Contains(IonValue item)
        {
            if (NullFlagOn() || item == null)
                return false;

            return item.Container == this;
        }

        public IonValue this[int index]
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

        public override IEnumerator<IonValue> GetEnumerator()
        {
            ThrowIfNull();
            return _children.GetEnumerator();
        }
    }
}
