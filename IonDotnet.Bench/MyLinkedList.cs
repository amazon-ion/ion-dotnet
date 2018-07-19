using System.Collections;
using System.Collections.Generic;

namespace IonDotnet.Bench
{
    public class Node<T>
    {
        public T Value;
        public Node<T> Next;

        public Node(T value)
        {
            Value = value;
        }
    }

    public struct MyLinkedList<T> : IEnumerable<T>
    {
        public Node<T> First;
        public Node<T> Last;

        public MyLinkedList(T value)
        {
            First = new Node<T>(value);
            Last = First;
        }

        public void Add(T item)
        {
            if (IsEmpty)
            {
                First = new Node<T>(item);
                Last = First;
                return;
            }

            Last.Next = new Node<T>(item);
            Last = Last.Next;
        }

        public void RemoveFirst()
        {
            if (First == null) return;
            First = First.Next;
        }

        public bool IsEmpty => First == null;

        public void Append(MyLinkedList<T> other)
        {
            if (other.IsEmpty) return;

            if (IsEmpty)
            {
                First = other.First;
                Last = other.Last;
                return;
            }

            Last.Next = other.First;
        }

        public IEnumerator<T> GetEnumerator()
        {
            var pt = First;
            while (pt != null)
            {
                yield return pt.Value;
                pt = pt.Next;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
