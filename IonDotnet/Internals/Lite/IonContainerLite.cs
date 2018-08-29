using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace IonDotnet.Internals.Lite
{
    internal abstract class IonContainerLite : IonValueLite, IPrivateIonContainer, IContext
    {
        protected int ChildCount;
        protected IonValueLite[] Children;

        protected IonContainerLite(ContainerlessContext containerlessContext, bool isNull) : base(containerlessContext, isNull)
        {
        }

        protected IonContainerLite(IonContainerLite existing, IContext context, bool isStruct) : base(existing, context)
        {
            ChildCount = existing.ChildCount;
            if (existing.Children == null) return;

            var isDatagram = this is IonDatagramLite;
            Children = new IonValueLite[ChildCount];
            for (var i = 0; i < existing.Children.Length; i++)
            {
                var child = existing.Children[i];
                var childContext = isDatagram
                    ? new TopLevelContext(child.GetAssignedSymbolTable(), this as IonDatagramLite)
                    : this as IContext;
                var copy = child.Clone(childContext);
                if (isStruct)
                {
                    if (child.FieldName == null)
                    {
                        // TODO figure out what this is
                        // when name is null it could be a sid 0 so we need to perform the full
                        // symbol token lookup.
                        // this is expensive so only do it when necessary
                        copy.FieldNameSymbol = child.GetKnownFieldNameSymbol();
                    }
                    else
                    {
                        copy.FieldName = child.FieldName;
                    }
                }

                Children[i] = copy;
            }
        }

        public IEnumerator<IIonValue> GetEnumerator()
        {
            if (Children == null) yield break;

            foreach (var child in Children)
            {
                yield return child;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public virtual void Add(IIonValue item)
        {
            if (!(item is IonValueLite ionValueLite))
            {
                throw new InvalidOperationException();
            }

            AddChild(ChildCount, ionValueLite);
        }

        public void Clear()
        {
            CheckLocked();

            if (IsNullValue())
            {
                IsNullValue(false);
            }
            else if (Count != 0)
            {
                DetachAllChildren();
                ChildCount = 0;
            }
        }

        public bool Contains(IIonValue item)
        {
            if (!(item is IonValueLite lite)) return false;
            return lite.ElementId >= 0
                   && lite.ElementId < ChildCount
                   && ReferenceEquals(item, Children[lite.ElementId]);
        }

        public void CopyTo(IIonValue[] array, int arrayIndex)
        {
            var idx = 0;
            while (idx < ChildCount && arrayIndex < array.Length)
            {
                array[arrayIndex] = Children[idx].Clone(new ContainerlessContext(GetIonSystemLite()));
                idx++;
                arrayIndex++;
            }
        }

        public bool Remove(IIonValue item)
        {
            CheckLocked();
            if (!(item is IonValueLite concrete) || !ReferenceEquals(concrete.Container, this)) return false;

            var pos = concrete.ElementId;
            var child = GetChild(pos);
            if (!ReferenceEquals(concrete, child)) throw new IonException("Element index is incorrect");

            RemoveChild(pos);
            PatchElement(pos);
            return true;
        }

        private void RemoveChild(int idx)
        {
            Debug.Assert(idx >= 0 && idx < ChildCount);
            Debug.Assert(GetChild(idx) != null);

            Children[idx].DetachFromContainer();
            var childrenToMove = ChildCount - idx - 1;
            if (childrenToMove > 0)
            {
                Array.Copy(Children, idx + 1, Children, idx, childrenToMove);
            }

            ChildCount--;
            Children[ChildCount] = null;
        }

        public int Count => IsNull ? 0 : ChildCount;

        public bool IsReadOnly => ReadOnly;

        public void MakeNull()
        {
            Clear();
            IsNullValue(true);
        }

//        public bool IsEmpty { get; }

        public int GetChildCount() => ChildCount;

        public IIonValue GetChild(int index)
        {
            if (index < 0 || index >= ChildCount) throw new ArgumentOutOfRangeException($"ChildCount: {ChildCount}");

            return Children[index];
        }

        public IonContainerLite GetContextContainer() => this;

        public IonSystemLite GetSystem() => GetIonSystemLite();


        /// <remarks>Null since symbol tables are only directly assigned to top-level values.</remarks>
        public ISymbolTable GetContextSymbolTable() => null;

        private void DetachAllChildren()
        {
            for (var ii = 0; ii < ChildCount; ii++)
            {
                var child = Children[ii];
                child.DetachFromContainer();
                Children[ii] = null;
            }
        }

        /// <summary>
        /// patch the element Id's for all the children from the child who was earliest in the array (lowest index)
        /// </summary>
        private void PatchElement(int lowestBadIdx)
        {
            for (var ii = lowestBadIdx; ii < ChildCount; ii++)
            {
                var child = GetChild(ii) as IonValueLite;
                Debug.Assert(child != null);
                child.SetElementId(ii);
            }
        }

        protected void Add(int index, IonValueLite item)
        {
            if (index < 0 || index > ChildCount) throw new IndexOutOfRangeException();
            CheckLocked();
            ValidateNewChild(item);
            AddChild(index, item);
            PatchElement(index + 1);
        }

        private static void ValidateNewChild(IonValueLite child)
        {
            if (child.Container != null) throw new ContainedValueException();
            if (child.ReadOnly) throw new InvalidOperationException("IonContainer is read-only");

            if (child is IIonDatagram) throw new InvalidOperationException("IonDatagram can not be inserted into another IonContainer.");
        }

        protected int AddChild(int index, IonValueLite child)
        {
            IsNullValue(false);
            child.SetContext(this);
            if (Children == null || ChildCount >= Children.Length)
            {
                var oldLength = Children?.Length ?? 0;
                var newLength = NextSize(oldLength, true);
                Array.Resize(ref Children, newLength);
            }

            if (index < ChildCount)
            {
                Array.Copy(Children, index, Children, index + 1, ChildCount - index);
            }

            ChildCount++;
            Children[index] = child;

            child.SetElementId(index);
            return index;
        }

        private static int InitialSize(IonType type)
        {
            switch (type)
            {
                case IonType.List:
                    return 1;
                case IonType.Sexp:
                    return 4;
                case IonType.Struct:
                    return 5;
                case IonType.Datagram:
                    return 3;
                default:
                    return 4;
            }
        }

        protected int NextSize(int currentSize, bool callTransition)
        {
            if (currentSize == 0)
            {
                var newSize = InitialSize(Type);
                return newSize;
            }

            int nextSize;
            switch (Type)
            {
                case IonType.List:
                    nextSize = 4;
                    break;
                case IonType.Sexp:
                    nextSize = 8;
                    break;
                case IonType.Struct:
                    nextSize = 8;
                    break;
                case IonType.Datagram:
                    nextSize = 10;
                    break;
                default:
                    return currentSize * 2;
            }

            if (nextSize > currentSize)
            {
                // note that unrecognized sizes, either due to unrecognized type id
                // or some sort of custom size in the initial allocation, meh.
                if (callTransition)
                {
                    TransitionToLargeSize(nextSize);
                }
            }
            else
            {
                nextSize = currentSize * 2;
            }

            return nextSize;
        }

        protected virtual void TransitionToLargeSize(int size)
        {
        }
    }
}
