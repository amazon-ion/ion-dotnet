using System;
using System.Diagnostics;

namespace IonDotnet.Tree.Impl
{
    /// <inheritdoc />
    /// <summary>
    /// An ion datagram is a special kind of value which represents a stream of Ion values.
    /// </summary>
    internal sealed class IonDatagram : IonSequence, IIonDatagram
    {
        public IonDatagram() : base(false)
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Use strict reference equality for datagram.
        /// </summary>
        public override bool IsEquivalentTo(IIonValue other) => other == this;

        public override IonType Type() => IonType.Datagram;

        //public override IIonContainer Container //?
        //{
        //    get => null;
        //    set => throw new InvalidOperationException("Cannot set the container of an Ion Datagram");
        //}

        /// <summary>
        /// Adding an item to the datagram will mark the current symbol table.
        /// </summary>
        public override void Add(IIonValue item)
        {
            base.Add(item);
            Debug.Assert(item != null, nameof(item) + " != null");
        }

        public int GetHashCode(IonValue obj) => obj.GetHashCode();
    }
}
