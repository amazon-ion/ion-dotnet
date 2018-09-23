using System;
using System.Collections.Generic;
using System.Diagnostics;
using IonDotnet.Internals;

namespace IonDotnet.Tree
{
    public sealed class IonStruct : IonContainer
    {
        private Dictionary<string, IonValue> _dictionary;

        public IonStruct(bool isNull) : base(isNull)
        {
            if (!isNull)
            {
                _dictionary = new Dictionary<string, IonValue>();
            }
        }

        internal override void WriteBodyTo(IPrivateWriter writer)
        {
            throw new System.NotImplementedException();
        }

        public override IonType Type => IonType.Struct;

        public override void Add(IonValue item)
            => throw new NotSupportedException("Cannot add a value to a struct without field name");

        public override void Clear()
        {
            ThrowIfLocked();
            if (NullFlagOn() && _dictionary == null)
                _dictionary = new Dictionary<string, IonValue>();

            NullFlagOn(false);
            foreach (var kvp in _dictionary)
            {
                kvp.Value.Container = null;
            }

            _dictionary.Clear();
        }

        public override bool Contains(IonValue item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            ThrowIfNull();
            Debug.Assert(_dictionary != null);

            return _dictionary.ContainsValue(item);
        }

        public override IEnumerator<IonValue> GetEnumerator()
        {
            if (NullFlagOn())
                yield break;

            Debug.Assert(_dictionary != null);
            foreach (var kvp in _dictionary)
            {
                yield return kvp.Value;
            }
        }

        public override bool Remove(IonValue item)
        {
            ThrowIfNull();
            ThrowIfLocked();
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (item.Container != this)
                return false;

            Debug.Assert(item.FieldName != null && _dictionary != null);
            _dictionary.Remove(item.FieldName);
            item.Container = null;
            item.FieldName = null;
            return true;
        }

        /// <returns>True if the struct contains such field name.</returns>
        public bool ContainsField(string fieldName)
            => _dictionary != null && _dictionary.ContainsKey(fieldName);

        public IonValue this[string fieldName]
        {
            get
            {
                ThrowIfNull();
                return _dictionary[fieldName];
            }
            set
            {
                ThrowIfLocked();
                ThrowIfNull();
                if (value.Container != null)
                    throw new ContainedValueException();

                value.FieldName = fieldName;
                _dictionary[fieldName] = value;
                value.Container = this;
            }
        }

        public override int Count => _dictionary?.Count ?? 0;
    }
}
