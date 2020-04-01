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
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Text;
    using Amazon.IonDotnet.Builders;
    using Amazon.IonDotnet.Internals;
    using Amazon.IonDotnet.Internals.Text;

    /// <summary>
    /// Represents a tree view into Ion data. Each <see cref="IonValue" /> is a node in the tree. These values are
    /// mutable and strictly hierarchical.
    /// </summary>
    internal abstract class IonValue : IIonValue
    {
        private const byte LockedFlag = 0x01;
        private const byte SystemValueFlag = 0x02;
        private const byte NullFlag = 0x04;
        private const byte BoolTrueFlag = 0x08;
        private const byte IvmFlag = 0x10;
        private const byte AutoCreatedFlag = 0x20;
        private const byte SymbolPresentFlag = 0x80;

        /// <summary>
        /// This field stores information about different value properties, and element Id
        /// First byte for flags.
        /// <para>The rest 24bit for element id.</para>
        /// </summary>
        private byte flags;
        private List<SymbolToken> annotations;

        protected IonValue(bool isNull)
        {
            if (isNull)
            {
                this.NullFlagOn(true);
            }
        }

        /// <summary>
        /// Gets or sets the field name text and sid.
        /// </summary>
        public SymbolToken FieldNameSymbol { get; set; }

        /// <summary>
        /// Gets a value indicating whether this value is a null value.
        /// </summary>
        public bool IsNull => this.NullFlagOn();

        public bool IsReadOnly => this.LockedFlagOn();

        // Applicable to IonDecimal
        public virtual decimal DecimalValue
        {
            get => throw new InvalidOperationException(this.GetErrorMessage());
        }

        public virtual BigDecimal BigDecimalValue
        {
            get => throw new InvalidOperationException(this.GetErrorMessage());
        }

        // Applicable to IonContainer
        public virtual int Count => throw new InvalidOperationException(this.GetErrorMessage());

        // Applicable to IonText
        public virtual string StringValue
        {
            get => throw new InvalidOperationException(this.GetErrorMessage());
        }

        // Applicable to IonInt
        public virtual IntegerSize IntegerSize => throw new InvalidOperationException(this.GetErrorMessage());

        public virtual BigInteger BigIntegerValue
        {
            get => throw new InvalidOperationException(this.GetErrorMessage());
        }

        public virtual int IntValue
        {
            get => throw new InvalidOperationException(this.GetErrorMessage());
        }

        public virtual long LongValue
        {
            get => throw new InvalidOperationException(this.GetErrorMessage());
        }

        // Applicable to IonSymbol
        public virtual SymbolToken SymbolValue
        {
            get => throw new InvalidOperationException(this.GetErrorMessage());
        }

        // Applicable to IonBool
        public virtual bool BoolValue
        {
            get => throw new InvalidOperationException(this.GetErrorMessage());
        }

        // Applicable to IonFloat
        public virtual double DoubleValue
        {
            get => throw new InvalidOperationException(this.GetErrorMessage());
        }

        // Applicable to IonTimestamp
        public virtual Timestamp TimestampValue
        {
            get => throw new InvalidOperationException(this.GetErrorMessage());
        }

        /// <summary>
        /// Get this value's user type annotations.
        /// </summary>
        /// <returns>Read-only collection of type annotations.</returns>
        public IReadOnlyCollection<SymbolToken> GetTypeAnnotationSymbols()
        {
            if (this.annotations == null)
            {
                return SymbolToken.EmptyArray;
            }

            return this.annotations;
        }

        /// <summary>
        /// Add an annotation to this value.
        /// </summary>
        /// <param name="annotation">Annotation text.</param>
        /// <exception cref="ArgumentNullException">When annotation is null.</exception>
        public void AddTypeAnnotation(string annotation)
        {
            if (annotation == null)
            {
                throw new ArgumentNullException(nameof(annotation));
            }

            this.AddTypeAnnotation(new SymbolToken(annotation, SymbolToken.UnknownSid));
        }

        /// <summary>
        /// Add an annotation to this value.
        /// </summary>
        /// <param name="annotation">Annotation symbol.</param>
        public void AddTypeAnnotation(SymbolToken annotation)
        {
            this.ThrowIfLocked();

            if (this.annotations == null)
            {
                this.annotations = new List<SymbolToken>(1);
            }

            this.annotations.Add(annotation);
        }

        /// <summary>
        /// Clear all annotations of this value.
        /// </summary>
        public void ClearAnnotations()
        {
            this.ThrowIfLocked();
            if (this.annotations == null)
            {
                this.annotations = new List<SymbolToken>();
            }

            this.annotations.Clear();
        }

        /// <summary>
        /// Returns true if the value contains such annotation.
        /// </summary>
        /// <param name="text">Annotation text.</param>
        /// <returns>True if the value contains such annotation.</returns>
        /// <exception cref="ArgumentNullException">When text is null.</exception>
        public bool HasAnnotation(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            return this.annotations != null && this.annotations.Any(a => text.Equals(a.Text));
        }

        /// <summary>
        /// Make this value become a null.
        /// </summary>
        public virtual void MakeNull()
        {
            this.ThrowIfLocked();
            this.NullFlagOn(true);
        }

        public void WriteTo(IIonWriter writer)
        {
            if (!(writer is IPrivateWriter privateWriter))
            {
                throw new InvalidOperationException();
            }

            if (writer.IsInStruct && !privateWriter.IsFieldNameSet())
            {
                if (this.FieldNameSymbol == default)
                {
                    throw new IonException("Field name is not set");
                }

                writer.SetFieldNameSymbol(this.FieldNameSymbol);
            }

            privateWriter.ClearTypeAnnotations();
            if (this.annotations != null)
            {
                foreach (var a in this.annotations)
                {
                    privateWriter.AddTypeAnnotationSymbol(a);
                }
            }

            this.WriteBodyTo(privateWriter);
        }

        public void MakeReadOnly() => this.LockedFlagOn(true);

        public string ToPrettyString()
        {
            using (var sw = new StringWriter())
            {
                var writer = new IonTextWriter(sw, new IonTextOptions { PrettyPrint = true });
                this.WriteTo(writer);
                writer.Finish();
                return sw.ToString();
            }
        }

        public virtual bool IsEquivalentTo(IIonValue value)
        {
            var valueIonValue = (IonValue)value;
            return this.IsEquivalentTo(valueIonValue);
        }

        public virtual ReadOnlySpan<byte> Bytes()
        {
            throw new InvalidOperationException(this.GetErrorMessage());
        }

        public virtual void SetBytes(ReadOnlySpan<byte> buffer)
        {
            throw new InvalidOperationException(this.GetErrorMessage());
        }

        public virtual int ByteSize()
        {
            throw new InvalidOperationException(this.GetErrorMessage());
        }

        public virtual StreamReader NewReader(Encoding encoding)
        {
            throw new InvalidOperationException(this.GetErrorMessage());
        }

        public virtual void RemoveAt(int index)
        {
            throw new InvalidOperationException(this.GetErrorMessage());
        }

        public virtual IIonValue GetElementAt(int index)
        {
            throw new InvalidOperationException(this.GetErrorMessage());
        }

        public virtual bool ContainsField(string fieldName)
        {
            throw new InvalidOperationException(this.GetErrorMessage());
        }

        public virtual IIonValue GetField(string fieldName)
        {
            throw new InvalidOperationException(this.GetErrorMessage());
        }

        public virtual void SetField(string fieldName, IIonValue value)
        {
            throw new InvalidOperationException(this.GetErrorMessage());
        }

        public virtual bool RemoveField(string fieldName)
        {
            throw new InvalidOperationException(this.GetErrorMessage());
        }

        public virtual void Clear()
        {
            throw new InvalidOperationException(this.GetErrorMessage());
        }

        public virtual void Add(IIonValue item)
        {
            throw new InvalidOperationException(this.GetErrorMessage());
        }

        public virtual int IndexOf(IIonValue item)
        {
            throw new InvalidOperationException(this.GetErrorMessage());
        }

        public virtual bool Remove(IIonValue item)
        {
            throw new InvalidOperationException(this.GetErrorMessage());
        }

        public virtual bool Contains(IIonValue item)
        {
            throw new InvalidOperationException(this.GetErrorMessage());
        }

        public virtual void CopyTo(IIonValue[] array, int arrayIndex)
        {
            throw new InvalidOperationException(this.GetErrorMessage());
        }

        public virtual IEnumerator<IIonValue> GetEnumerator()
        {
            throw new InvalidOperationException(this.GetErrorMessage());
        }

        public virtual IonType Type()
        {
            throw new InvalidOperationException("This operation is not supported for this IonType}");
        }

        /// <summary>
        /// Concrete class implementations should call the correct writer method.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        internal abstract void WriteBodyTo(IPrivateWriter writer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected int GetMetadata(int mask, int shift) => (this.flags & mask) >> shift;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void SetMetadata(int metadata, byte mask, int shift)
        {
            this.flags &= (byte)~mask;
            this.flags |= (byte)((metadata << shift) & mask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool BoolTrueFlagOn() => this.HasFlag(BoolTrueFlag);

        protected void BoolTrueFlagOn(bool value)
        {
            if (value)
            {
                this.SetFlag(BoolTrueFlag);
            }
            else
            {
                this.ClearFlag(BoolTrueFlag);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool IsSystemValue() => this.HasFlag(SystemValueFlag);

        protected bool IsSystemValue(bool value)
        {
            if (value)
            {
                this.SetFlag(SystemValueFlag);
            }
            else
            {
                this.ClearFlag(SystemValueFlag);
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool NullFlagOn() => this.HasFlag(NullFlag);

        protected void NullFlagOn(bool value)
        {
            if (value)
            {
                this.SetFlag(NullFlag);
            }
            else
            {
                this.ClearFlag(NullFlag);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool IsIvm() => this.HasFlag(IvmFlag);

        protected bool IsIvm(bool value)
        {
            if (value)
            {
                this.SetFlag(IvmFlag);
            }
            else
            {
                this.ClearFlag(IvmFlag);
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool IsAutoCreated() => this.HasFlag(AutoCreatedFlag);

        protected bool IsAutoCreated(bool value)
        {
            if (value)
            {
                this.SetFlag(AutoCreatedFlag);
            }
            else
            {
                this.ClearFlag(AutoCreatedFlag);
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool IsSymbolPresent() => this.HasFlag(SymbolPresentFlag);

        protected bool IsSymbolPresent(bool value)
        {
            if (value)
            {
                this.SetFlag(SymbolPresentFlag);
            }
            else
            {
                this.ClearFlag(SymbolPresentFlag);
            }

            return value;
        }

        protected void ThrowIfLocked()
        {
            if (this.LockedFlagOn())
            {
                throw new InvalidOperationException("Value is read-only");
            }
        }

        protected void ThrowIfNull()
        {
            if (this.NullFlagOn())
            {
                throw new NullValueException();
            }
        }

        private string GetErrorMessage()
        {
            return $"This operation is not supported for IonType {this.Type()}";
        }

        /// <summary>
        /// Returns true if this value is equivalent to the other, false otherwise.
        /// </summary>
        /// <param name="other">The other value.</param>
        /// <remarks>
        /// Equivalency is determined by whether the <see cref="IonValue"/> objects has the same annotations,
        /// hold equal values, or in the case of container, they contain equivalent sets of children.
        /// </remarks>
        private bool IsEquivalentTo(IonValue other)
        {
            if (other == null || this.Type() != other.Type())
            {
                return false;
            }

            var otherAnnotations = other.annotations;
            if (this.annotations == null)
            {
                return otherAnnotations == null || otherAnnotations.Count == 0;
            }

            if (otherAnnotations == null || otherAnnotations.Count != this.annotations.Count)
            {
                return false;
            }

            for (int i = 0, l = this.annotations.Count; i < l; i++)
            {
                if (!this.annotations[i].IsEquivalentTo(otherAnnotations[i]))
                {
                    return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasFlag(byte flagBit) => (this.flags & flagBit) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetFlag(byte flagBit) => this.flags |= flagBit;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearFlag(byte flagBit) => this.flags &= (byte)~flagBit;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool LockedFlagOn() => this.HasFlag(LockedFlag);

        private void LockedFlagOn(bool value)
        {
            if (value)
            {
                this.SetFlag(LockedFlag);
            }
            else
            {
                this.ClearFlag(LockedFlag);
            }
        }
    }
}
