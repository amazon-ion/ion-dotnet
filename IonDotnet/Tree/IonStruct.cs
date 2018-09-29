using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using IonDotnet.Internals;

namespace IonDotnet.Tree
{
    public sealed class IonStruct : IonContainer
    {
        private List<IonValue> _values;

        public IonStruct() : this(false)
        {
        }

        private IonStruct(bool isNull) : base(isNull)
        {
            if (!isNull)
            {
                _values = new List<IonValue>();
            }
        }

        /// <summary>
        /// Returns a new null.struct value.
        /// </summary>
        public static IonStruct NewNull() => new IonStruct(true);

        public override bool IsEquivalentTo(IonValue other)
        {
            if (!(other is IonStruct otherStruct))
                return false;
            if (NullFlagOn())
                return other.IsNull;
            if (other.IsNull || otherStruct.Count != Count)
                return false;

            foreach (var thisKvp in _values)
            {
                throw new NotImplementedException();
//                if (!otherStruct._dictionary.TryGetValue(thisKvp.Key, out var otherValue))
//                    return false;
//                if (!otherValue.IsEquivalentTo(thisKvp.Value))
//                    return false;
            }

            return true;
        }

        internal override void WriteBodyTo(IPrivateWriter writer)
        {
            if (NullFlagOn())
            {
                writer.WriteNull(IonType.Struct);
                return;
            }

            Debug.Assert(_values != null);
            writer.StepIn(IonType.Struct);
            foreach (var v in _values)
            {
                //writeto() will attemp to write field name
                v.WriteTo(writer);
            }

            writer.StepOut();
        }

        public override IonType Type => IonType.Struct;

        public override void Add(IonValue item)
            => throw new NotSupportedException("Cannot add a value to a struct without field name");

        public override void Clear()
        {
            ThrowIfLocked();
            if (NullFlagOn() && _values == null)
                _values = new List<IonValue>();

            NullFlagOn(false);
            foreach (var v in _values)
            {
                v.Container = null;
            }

            _values.Clear();
        }

        public override bool Contains(IonValue item)
        {
            if (NullFlagOn() || item is null)
                return false;

            Debug.Assert(_values != null);
            return item.Container == this;
        }

        public override IEnumerator<IonValue> GetEnumerator()
        {
            if (NullFlagOn())
                yield break;

            foreach (var v in _values)
            {
                yield return v;
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

            Debug.Assert(item.FieldNameSymbol != default && _values != null);
            _values.Remove(item);
            item.Container = null;
            item.FieldNameSymbol = default;
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


            return RemoveUnsafe(fieldName) > 0;
        }

        /// <returns>True if the struct contains such field name.</returns>
        public bool ContainsField(string fieldName)
            => _values != null && _values.Any(v => v.FieldNameSymbol.Text == fieldName);

        /// <summary>
        /// Get or set the value with the field name. The getter will return the first value with a matched field name.
        /// The setter will replace all values with that field name with the new value.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <exception cref="ArgumentNullException">When field name is null.</exception>
        /// <exception cref="ContainedValueException">If the value being set is already contained in another container.</exception>
        public IonValue this[string fieldName]
        {
            get
            {
                if (fieldName is null)
                    throw new ArgumentNullException(nameof(fieldName));
                ThrowIfNull();
                return _values.FirstOrDefault(v => v.FieldNameSymbol.Text == fieldName);
            }
            set
            {
                if (fieldName is null)
                    throw new ArgumentNullException(nameof(fieldName));
                ThrowIfLocked();
                ThrowIfNull();
                if (value.Container != null)
                    throw new ContainedValueException();

                RemoveUnsafe(fieldName);

                value.FieldNameSymbol = new SymbolToken(fieldName, SymbolToken.UnknownSid);
                value.Container = this;
                _values.Add(value);
            }
        }

        public override int Count => _values?.Count ?? 0;

        private int RemoveUnsafe(string fieldName)
        {
            var ret = _values.RemoveAll(v =>
            {
                var match = v.FieldNameSymbol.Text == fieldName;
                if (match)
                {
                    v.Container = null;
                    v.FieldNameSymbol = default;
                }

                return match;
            });
            return ret;
        }
    }
}
