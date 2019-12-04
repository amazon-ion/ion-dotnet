using System;
using System.Collections;
using System.Collections.Generic;

namespace IonDotnet.Tree.Impl
{
    /// <inheritdoc cref="IonValue" />
    /// <summary>
    /// Base class for all container type (List,Struct,Sexp,Datagram) Ion values.
    /// This class also implements the <see cref="ICollection"/> interface.
    /// </summary>
    public abstract class IonContainer : IonValue, ICollection<IonValue>, IIonContainer
    {
        protected IonContainer(bool isNull) : base(isNull)
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Add an Ion value to the container.
        /// </summary>
        /// <param name="item">Ion value.</param>
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

        /// <inheritdoc />
        /// <summary>
        /// Returns true if the container contains an Ion value.
        /// </summary>
        /// <param name="item">Ion value.</param>
        public abstract bool Contains(IonValue item);

        /// <inheritdoc />
        /// <summary>
        /// This operation is not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">This operation is not supported.</exception>
        public void CopyTo(IonValue[] array, int arrayIndex) => throw new NotSupportedException();

        public override void MakeNull()
        {
            Clear();
            base.MakeNull();
        }

        public abstract IEnumerator<IonValue> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
