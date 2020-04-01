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

namespace Amazon.IonDotnet.Tree.Impl
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Amazon.IonDotnet.Internals;

    internal sealed class IonStruct : IonContainer, IIonStruct
    {
        private List<IIonValue> values;

        public IonStruct()
            : this(false)
        {
        }

        private IonStruct(bool isNull)
            : base(isNull)
        {
            if (!isNull)
            {
                this.values = new List<IIonValue>();
            }
        }

        public override int Count => this.values?.Count ?? 0;

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
                {
                    throw new ArgumentNullException(nameof(fieldName));
                }

                this.ThrowIfNull();
                return this.values.FirstOrDefault(v => v.FieldNameSymbol.Text == fieldName);
            }

            set
            {
                if (fieldName is null)
                {
                    throw new ArgumentNullException(nameof(fieldName));
                }

                this.ThrowIfLocked();
                this.ThrowIfNull();

                this.RemoveUnsafe(fieldName);

                value.FieldNameSymbol = new SymbolToken(fieldName, SymbolToken.UnknownSid);
                this.values.Add(value);
            }
        }

        /// <summary>
        /// Returns a new null.struct value.
        /// </summary>
        /// <returns>A null IonStruct.</returns>
        public static IonStruct NewNull() => new IonStruct(true);

        /// <inheritdoc />
        /// <remarks>
        /// Struct equivalence is an expensive operation.
        /// </remarks>
        public override bool IsEquivalentTo(IIonValue other)
        {
            if (!base.IsEquivalentTo(other))
            {
                return false;
            }

            if (!(other is IonStruct otherStruct))
            {
                return false;
            }

            if (this.NullFlagOn())
            {
                return other.IsNull;
            }

            if (other.IsNull || otherStruct.Count != this.Count)
            {
                return false;
            }

            if (this.values.Count != otherStruct.values.Count)
            {
                return false;
            }

            var multiset = this.ToMultiset();
            foreach (var v2 in otherStruct.values)
            {
                var field = new MultisetField(new SymbolToken(v2.FieldNameSymbol.Text, v2.FieldNameSymbol.Sid), v2);
                if (!multiset.TryGetValue(field, out var mapped) || mapped.Count == 0)
                {
                    return false;
                }

                mapped.Count--;
            }

            return true;
        }

        public override IonType Type() => IonType.Struct;

        public override void Add(IIonValue item)
            => throw new NotSupportedException("Cannot add a value to a struct without field name");

        /// <summary>
        /// Add a new value to this struct.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="value">Ion value to add.</param>
        /// <exception cref="ArgumentNullException">When field name is null.</exception>
        public void Add(string fieldName, IIonValue value)
        {
            if (fieldName is null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            this.ThrowIfLocked();
            this.ThrowIfNull();

            value.FieldNameSymbol = new SymbolToken(fieldName, SymbolToken.UnknownSid);
            this.values.Add(value);
        }

        public void Add(SymbolToken symbol, IIonValue value)
        {
            if (symbol.Text != null)
            {
                this.Add(symbol.Text, value);
                return;
            }

            if (symbol.Sid < 0)
            {
                throw new ArgumentException("symbol has no text or sid", nameof(symbol));
            }

            this.ThrowIfLocked();
            this.ThrowIfNull();

            value.FieldNameSymbol = symbol;
            this.values.Add(value);
        }

        public override void Clear()
        {
            this.ThrowIfLocked();
            if (this.NullFlagOn() && this.values == null)
            {
                this.values = new List<IIonValue>();
            }

            this.NullFlagOn(false);
            this.values.Clear();
        }

        public override bool Contains(IIonValue item)
        {
            if (this.NullFlagOn() || item is null)
            {
                return false;
            }

            Debug.Assert(this.values != null, "values is null");
            return this.values.Contains(item);
        }

        public override IEnumerator<IIonValue> GetEnumerator()
        {
            if (this.NullFlagOn())
            {
                yield break;
            }

            foreach (var v in this.values)
            {
                yield return v;
            }
        }

        public override IIonValue GetElementAt(int index)
        {
            return this.values.ElementAt(index);
        }

        public override bool Remove(IIonValue item)
        {
            this.ThrowIfNull();
            this.ThrowIfLocked();
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            Debug.Assert(item.FieldNameSymbol != default && this.values != null, "FieldNameSymbol is default or values is null");
            this.values.Remove(item);
            item.FieldNameSymbol = default;
            return true;
        }

        /// <summary>Returns whether or not the struct contains a field name.</summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>True if the struct contains such field name.</returns>
        public override bool ContainsField(string fieldName)
            => this.values != null && this.values.Any(v => v.FieldNameSymbol.Text == fieldName);

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
            this.ThrowIfNull();
            this.ThrowIfLocked();
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            return this.RemoveUnsafe(fieldName) > 0;
        }

        internal override void WriteBodyTo(IPrivateWriter writer)
        {
            if (this.NullFlagOn())
            {
                writer.WriteNull(IonType.Struct);
                return;
            }

            Debug.Assert(this.values != null, "values is null");
            writer.StepIn(IonType.Struct);
            foreach (var v in this.values)
            {
                v.WriteTo(writer);
            }

            writer.StepOut();
        }

        private int RemoveUnsafe(string fieldName)
        {
            var ret = this.values.RemoveAll(v =>
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
            Debug.Assert(this.values != null, "values is null");
            var dict = new Dictionary<MultisetField, MultisetField>();
            foreach (var v in this.values)
            {
                var field = new MultisetField(new SymbolToken(v.FieldNameSymbol.Text, v.FieldNameSymbol.Sid), v)
                {
                    Count = 1,
                };

                // assume that for most cases, the field name is unique.
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
            public int Count;
            private readonly SymbolToken name;
            private readonly IIonValue value;

            public MultisetField(SymbolToken name, IIonValue value)
            {
                Debug.Assert(name != null, "name is null");
                this.name = name;
                this.value = value;
                this.Count = 0;
            }

            public override int GetHashCode()
            {
                Debug.Assert(this.name != null, "name is null");
                return this.name.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                var other = (MultisetField)obj;
                Debug.Assert(other != null, "other is null");
                return this.name == other.name && other.value.IsEquivalentTo(this.value);
            }
        }
    }
}
