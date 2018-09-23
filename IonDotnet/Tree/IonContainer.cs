using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace IonDotnet.Tree
{
    public abstract class IonContainer : IonValue, ICollection<IonValue>
    {
        protected IonContainer(bool isNull) : base(isNull)
        {
        }


        public abstract void Add(IonValue item);

        /// <inheritdoc />
        /// <summary>
        /// Remove the item from container.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>True if the item has been removed</returns>
        public abstract bool Remove(IonValue item);


        /// <inheritdoc />
        /// <summary>
        /// The number of children of this container.
        /// </summary>
        public abstract int Count { get; }


        /// <inheritdoc />
        /// <summary>
        /// Clear the content of this container.
        /// </summary>
        /// <remarks>
        /// If this container is null, it will be set to a non-null empty container.
        /// </remarks>
        public abstract void Clear();

        public abstract bool Contains(IonValue item);

        public void CopyTo(IonValue[] array, int arrayIndex) => throw new NotSupportedException();

        public abstract IEnumerator<IonValue> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
