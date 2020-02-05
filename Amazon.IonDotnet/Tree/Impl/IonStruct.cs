/*
 * Copyright 2020 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Amazon.IonDotnet.Internals;

namespace Amazon.IonDotnet.Tree.Impl
{
    internal sealed class IonStruct : IonContainer, IIonStruct
    {
        private List<IIonValue> _values;

        public IonStruct() : this(false)
        {
        }

        private IonStruct(bool isNull) : base(isNull)
        {
            if (!isNull)
            {
                _values = new List<IIonValue>();
            }
        }

        /// <summary>
        /// Returns a new null.struct value.
        /// </summary>
        public static IonStruct NewNull() => new IonStruct(true);

        /// <inheritdoc />
        /// <remarks>
        /// Struct equivalence is an expensive operation.
        /// </remarks>
        public override bool IsEquivalentTo(IIonValue other)
        {
            if (!base.IsEquivalentTo(other))
                return false;

            if (!(other is IonStruct otherStruct))
                return false;
            if (NullFlagOn())
                return other.IsNull;
            if (other.IsNull || otherStruct.Count != Count)
                return false;

            if (_values.Count != otherStruct._values.Count)
                return false;

            var multiset = ToMultiset();
            foreach (var v2 in otherStruct._values)
            {
                var field = new MultisetField(new SymbolToken(v2.FieldNameSymbol.Text, v2.FieldNameSymbol.Sid), v2);
                if (!multiset.TryGetValue(field, out var mapped) || mapped.Count == 0)
                    return false;
                mapped.Count--;
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

        public override IonType Type() => IonType.Struct;

        public override void Add(IIonValue item)
            => throw new NotSupportedException("Cannot add a value to a struct without field name");

        /// <summary>
        /// Add a new value to this struct.
        /// </summary>
        /// <param name="fieldName">Field name</param>
        /// <param name="value">Ion value to add.</param>
        /// <exception cref="ArgumentNullException">When field name is null.</exception>
        public void Add(string fieldName, IIonValue value)
        {
            if (fieldName is null)
                throw new ArgumentNullException(nameof(fieldName));
            ThrowIfLocked();
            ThrowIfNull();

            value.FieldNameSymbol = new SymbolToken(fieldName, SymbolToken.UnknownSid);
            _values.Add(value);
        }

        public void Add(SymbolToken symbol, IIonValue value)
        {
            if (symbol.Text != null)
            {
                Add(symbol.Text, value);
                return;
            }

            if (symbol.Sid < 0)
                throw new ArgumentException("symbol has no text or sid", nameof(symbol));
            ThrowIfLocked();
            ThrowIfNull();

            value.FieldNameSymbol = symbol;
            _values.Add(value);
        }

        public override void Clear()
        {
            ThrowIfLocked();
            if (NullFlagOn() && _values == null)
                _values = new List<IIonValue>();

            NullFlagOn(false);
            _values.Clear();
        }

        public override bool Contains(IIonValue item)
        {
            if (NullFlagOn() || item is null)
                return false;

            Debug.Assert(_values != null);
            return _values.Contains(item);
        }

        public override IEnumerator<IIonValue> GetEnumerator()
        {
            if (NullFlagOn())
                yield break;

            foreach (var v in _values)
            {
                yield return v;
            }
        }

        public override IIonValue GetElementAt(int index)
        {
            return _values.ElementAt(index);
        }

        public override bool Remove(IIonValue item)
        {
            ThrowIfNull();
            ThrowIfLocked();
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            Debug.Assert(item.FieldNameSymbol != default && _values != null);
            _values.Remove(item);
            item.FieldNameSymbol = default;
            return true;
        }

        /// <returns>True if the struct contains such field name.</returns>
        public override bool ContainsField(string fieldName)
            => _values != null && _values.Any(v => v.FieldNameSymbol.Text == fieldName);

        /// <summary>
        /// Get or set the value with the field name. The getter will return the first value with a matched field name.
        /// The setter will replace all values with that field name with the new value.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <exception cref="ArgumentNullException">When field name is null.</exception>
        public IIonValue this[string fieldName]
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

                RemoveUnsafe(fieldName);

                value.FieldNameSymbol = new SymbolToken(fieldName, SymbolToken.UnknownSid);
                _values.Add(value);
            }
        }

        public override IIonValue GetField(string fieldName)
        {
            return this[fieldName];
        }

        public override void SetField(string fieldName, IIonValue value)
        {
            this[fieldName] = value;
        }

        /// <summary>
        /// Remove a field from this struct.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <returns>True if the field was removed, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">When <paramref name="fieldName"/> is empty.</exception>
        public override bool RemoveField(string fieldName)
        {
            ThrowIfNull();
            ThrowIfLocked();
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentNullException(nameof(fieldName));


            return RemoveUnsafe(fieldName) > 0;
        }

        public override int Count => _values?.Count ?? 0;

        private int RemoveUnsafe(string fieldName)
        {
            var ret = _values.RemoveAll(v =>
            {
                var match = v.FieldNameSymbol.Text == fieldName;
                if (match)
                {
                    v.FieldNameSymbol = default;
                }

                return match;
            });
            return ret;
        }

        private IDictionary<MultisetField, MultisetField> ToMultiset()
        {
            Debug.Assert(_values != null);
            var dict = new Dictionary<MultisetField, MultisetField>();
            foreach (var v in _values)
            {
                var field = new MultisetField(new SymbolToken(v.FieldNameSymbol.Text, v.FieldNameSymbol.Sid), v)
                {
                    Count = 1
                };
                //we will assume that for most cases, the field name is unique.
                if (dict.TryGetValue(field, out var existing))
                {
                    existing.Count += 1;
                }
                else
                {
                    dict.Add(field, field);
                }
            }

            return dict;
        }

        /// <summary>
        /// This class holds a reference to an Ion value and a counter to the number of values
        /// equal to that value in the struct.
        /// </summary>
        private class MultisetField
        {
            private readonly SymbolToken _name;
            private readonly IIonValue _value;
            public int Count;

            public MultisetField(SymbolToken name, IIonValue value)
            {
                Debug.Assert(name != null);
                _name = name;
                _value = value;
                Count = 0;
            }

            public override int GetHashCode()
            {
                Debug.Assert(_name != null);
                return _name.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                var other = (MultisetField) obj;
                Debug.Assert(other != null);
                return _name == other._name && other._value.IsEquivalentTo(_value);
            }
        }
    }
}
