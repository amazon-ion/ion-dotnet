using System;
using System.Collections.Generic;
using System.Diagnostics;
using IonDotnet.Internals;

namespace IonDotnet.Tree
{
    public abstract class IonSequence : IonContainer, IList<IonValue>
    {
        protected IonSequence(bool isNull) : base(isNull)
        {
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

        public int IndexOf(IonValue item)
        {
            if (NullFlagOn())
                return -1;

            Debug.Assert(_children != null);
            return _children.IndexOf(item);
        }

        public void Insert(int index, IonValue item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            ThrowIfNull();
            if (index >= _children.Count)
                throw new IndexOutOfRangeException($"Container has only {_children.Count} children");

            //this will check for lock
            Remove(_children[index]);
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
    }
}
