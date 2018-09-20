using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace IonDotnet.Tree
{
    public abstract class IonContainer : IonValue, ICollection<IonValue>
    {
        protected List<IonValue> _children;

        protected IonContainer(bool isNull) : base(isNull)
        {
            if (!isNull)
            {
                _children = new List<IonValue>();
            }
        }

        public void Add(IonValue item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            ThrowIfLocked();
            ThrowIfNull();

            _children.Add(item);
        }

        /// <inheritdoc />
        /// <summary>
        /// Remove the item from container.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>True if the item has been removed</returns>
        public bool Remove(IonValue item)
        {
            ThrowIfLocked();
            if (NullFlagOn() || item?.Container != this)
                return false;

            Debug.Assert(_children?.Contains(item) == true);
            _children.Remove(item);
            item.Container = null;
            return true;
        }


        /// <summary>
        /// The number of children of this container.
        /// </summary>
        public int Count => _children.Count;


        /// <summary>
        /// Clear the content of this container.
        /// </summary>
        /// <remarks>
        /// If this container is null, it will be set to a non-null empty container.
        /// </remarks>
        public void Clear()
        {
            ThrowIfLocked();
            if (NullFlagOn())
            {
                _children = new List<IonValue>();
            }

            NullFlagOn(false);
            _children.Clear();
        }

        public bool Contains(IonValue item)
        {
            if (NullFlagOn() || item == null)
                return false;

            return item.Container == this;
        }

        public void CopyTo(IonValue[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<IonValue> GetEnumerator()
        {
            ThrowIfNull();
            return _children.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
