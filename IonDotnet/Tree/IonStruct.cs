using System;
using System.Collections.Generic;
using System.Diagnostics;
using IonDotnet.Internals;

namespace IonDotnet.Tree
{
    public sealed class IonStruct : IonContainer
    {
        private Dictionary<string, IonValue> _dictionary;

        public IonStruct() : this(false)
        {
        }

        private IonStruct(bool isNull) : base(isNull)
        {
            if (!isNull)
            {
                _dictionary = new Dictionary<string, IonValue>();
            }
        }

        /// <summary>
        /// Returns a new null.struct value.
        /// </summary>
        public static IonStruct NewNull() => new IonStruct(true);

        public override bool EqualsTo(IonValue other)
        {
            throw new NotImplementedException();
        }

        internal override void WriteBodyTo(IPrivateWriter writer)
        {
            if (NullFlagOn())
            {
                writer.WriteNull(IonType.Struct);
                return;
            }

            Debug.Assert(_dictionary != null);
            writer.StepIn(IonType.Struct);
            foreach (var kvp in _dictionary)
            {
                //writeto() will attemp to write field name
                kvp.Value.WriteTo(writer);
            }

            writer.StepOut();
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
            RemoveUnsafe(item.FieldName, item);
            return true;
        }

        /// <summary>
        /// Remove a field from this struct.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <returns>True if the field was removed, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">When <paramref name="fieldName"/> is empty.</exception>
        public bool RemoveField(string fieldName)
        {
            ThrowIfNull();
            ThrowIfLocked();
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentNullException(nameof(fieldName));

            if (!_dictionary.TryGetValue(fieldName, out var item))
                return false;
            RemoveUnsafe(fieldName, item);
            return true;
        }

        /// <returns>True if the struct contains such field name.</returns>
        public bool ContainsField(string fieldName)
            => _dictionary != null && _dictionary.ContainsKey(fieldName);

        public IonValue this[string fieldName]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(fieldName))
                    throw new ArgumentNullException(nameof(fieldName));
                ThrowIfNull();
                return _dictionary[fieldName];
            }
            set
            {
                if (string.IsNullOrWhiteSpace(fieldName))
                    throw new ArgumentNullException(nameof(fieldName));
                ThrowIfLocked();
                ThrowIfNull();
                if (value.Container != null)
                    throw new ContainedValueException();

                if (_dictionary.TryGetValue(fieldName, out var item))
                {
                    RemoveUnsafe(fieldName, item);
                }

                value.FieldName = fieldName;
                _dictionary[fieldName] = value;
                value.Container = this;
            }
        }

        public override int Count => _dictionary?.Count ?? 0;

        private void RemoveUnsafe(string fieldName, IonValue item)
        {
            _dictionary.Remove(fieldName);
            item.Container = null;
            item.FieldName = null;
        }
    }
}
