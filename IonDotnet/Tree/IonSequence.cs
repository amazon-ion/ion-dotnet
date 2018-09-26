using System;
using System.Collections.Generic;
using System.Diagnostics;
using IonDotnet.Internals;

namespace IonDotnet.Tree
{
    public abstract class IonSequence : IonContainer, IList<IonValue>
    {
        protected List<IonValue> Children;

        protected IonSequence(bool isNull) : base(isNull)
        {
            if (!isNull)
            {
                Children = new List<IonValue>();
            }
        }

        internal override void WriteBodyTo(IPrivateWriter writer)
        {
            if (NullFlagOn())
            {
                writer.WriteNull(Type);
                return;
            }

            Debug.Assert(Children != null);

            writer.StepIn(Type);
            foreach (var val in Children)
            {
                val.WriteTo(writer);
            }

            writer.StepOut();
        }

        /// <inheritdoc />
        /// <summary>
        /// The number of children of this container.
        /// </summary>
        public override int Count => Children?.Count ?? 0;

        public int IndexOf(IonValue item)
        {
            if (NullFlagOn())
                return -1;

            Debug.Assert(Children != null);
            return Children.IndexOf(item);
        }

        public override void Add(IonValue item)
        {
            ThrowIfLocked();
            ThrowIfNull();
            if (item is null)
                throw new ArgumentNullException(nameof(item));
            if (item.Container != null)
                throw new ContainedValueException(item);

            Children.Add(item);
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
            ThrowIfNull();
            if (NullFlagOn() || item?.Container != this)
                return false;

            Debug.Assert(Children?.Contains(item) == true);
            Children.Remove(item);
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
                throw new ContainedValueException(item);

            //this will check range
            Children.Insert(index, item);
            item.Container = this;
        }

        public void RemoveAt(int index)
        {
            ThrowIfNull();
            if (index >= Children.Count)
                throw new IndexOutOfRangeException($"Container has only {Children.Count} children");

            //this will check for lock
            Remove(Children[index]);
        }

        public override bool IsEquivalentTo(IonValue other)
        {
            if (other.Type != Type)
                return false;

            var otherSeq = (IonSequence) other;
            if (NullFlagOn())
                return otherSeq.IsNull;
            if (otherSeq.IsNull || otherSeq.Count != Count)
                return false;

            for (int i = 0, l = Count; i < l; i++)
            {
                if (!Children[i].IsEquivalentTo(otherSeq.Children[i]))
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
            if (NullFlagOn() && Children == null)
            {
                Children = new List<IonValue>();
            }

            foreach (var child in Children)
            {
                child.Container = null;
            }

            NullFlagOn(false);
            Children.Clear();
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
                return Children[index];
            }
            set
            {
                ThrowIfLocked();
                ThrowIfNull();
                Children[index] = value;
            }
        }

        public override IEnumerator<IonValue> GetEnumerator()
        {
            ThrowIfNull();
            return Children.GetEnumerator();
        }
    }
}
