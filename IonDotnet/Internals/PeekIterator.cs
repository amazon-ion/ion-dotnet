using System;
using System.Collections.Generic;

namespace IonDotnet.Internals
{
    internal class PeekIterator<T> : IIterator<T> where T : class
    {
        private readonly IEnumerator<T> _enumerator;
        private bool _ended;

        public PeekIterator(IEnumerable<T> enumerable)
        {
            _enumerator = enumerable.GetEnumerator();
            _ended = !_enumerator.MoveNext();
        }

        public bool HasNext() => !_ended;

        public T Next()
        {
            if (_ended) throw new InvalidOperationException();
            var current = _enumerator.Current;
            _ended = !_enumerator.MoveNext();

            return current;
        }

        public void Dispose()
        {
            _enumerator?.Dispose();
        }
    }
}
