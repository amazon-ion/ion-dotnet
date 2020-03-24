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
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Amazon.IonDotnet.Builders;
using Amazon.IonDotnet.Internals;
using Amazon.IonDotnet.Internals.Text;

namespace Amazon.IonDotnet.Tree.Impl
{
    /// <summary>
    /// Represents a tree view into Ion data. Each <see cref="IonValue" /> is a node in the tree. These values are
    /// mutable and strictly hierarchical.
    /// </summary>
    internal abstract class IonValue : IIonValue
    {
        #region Flags

        private const byte LockedFlag = 0x01;
        private const byte SystemValueFlag = 0x02;
        private const byte NullFlag = 0x04;
        private const byte BoolTrueFlag = 0x08;
        private const byte IvmFlag = 0x10;
        private const byte AutoCreatedFlag = 0x20;
        private const byte SymbolPresentFlag = 0x80;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected int GetMetadata(int mask, int shift) => (_flags & mask) >> shift;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void SetMetadata(int metadata, byte mask, int shift)
        {
            _flags &= (byte) ~mask;
            _flags |= (byte) ((metadata << shift) & mask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasFlag(byte flagBit) => (_flags & flagBit) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetFlag(byte flagBit) => _flags |= flagBit;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearFlag(byte flagBit) => _flags &= (byte) ~flagBit;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool LockedFlagOn() => HasFlag(LockedFlag);

        private void LockedFlagOn(bool value)
        {
            if (value)
            {
                SetFlag(LockedFlag);
            }
            else
            {
                ClearFlag(LockedFlag);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool BoolTrueFlagOn() => HasFlag(BoolTrueFlag);

        protected void BoolTrueFlagOn(bool value)
        {
            if (value)
            {
                SetFlag(BoolTrueFlag);
            }
            else
            {
                ClearFlag(BoolTrueFlag);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool IsSystemValue() => HasFlag(SystemValueFlag);

        protected bool IsSystemValue(bool value)
        {
            if (value)
            {
                SetFlag(SystemValueFlag);
            }
            else
            {
                ClearFlag(SystemValueFlag);
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool NullFlagOn() => HasFlag(NullFlag);

        protected void NullFlagOn(bool value)
        {
            if (value)
            {
                SetFlag(NullFlag);
            }
            else
            {
                ClearFlag(NullFlag);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool IsIvm() => HasFlag(IvmFlag);

        protected bool IsIvm(bool value)
        {
            if (value)
            {
                SetFlag(IvmFlag);
            }
            else
            {
                ClearFlag(IvmFlag);
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool IsAutoCreated() => HasFlag(AutoCreatedFlag);

        protected bool IsAutoCreated(bool value)
        {
            if (value)
            {
                SetFlag(AutoCreatedFlag);
            }
            else
            {
                ClearFlag(AutoCreatedFlag);
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool IsSymbolPresent() => HasFlag(SymbolPresentFlag);

        protected bool IsSymbolPresent(bool value)
        {
            if (value)
            {
                SetFlag(SymbolPresentFlag);
            }
            else
            {
                ClearFlag(SymbolPresentFlag);
            }

            return value;
        }

        protected void ThrowIfLocked()
        {
            if (LockedFlagOn())
                throw new InvalidOperationException("Value is read-only");
        }

        protected void ThrowIfNull()
        {
            if (NullFlagOn())
                throw new NullValueException();
        }

        /// <summary>
        /// This field stores information about different value properties, and element Id
        /// First byte for flags.
        /// <para>The rest 24bit for element id.</para>
        /// </summary>
        private byte _flags;

        #endregion

        private List<SymbolToken> _annotations;

        /// <summary>
        /// Store the field name text and sid.
        /// </summary>
        public SymbolToken FieldNameSymbol { get; set; }

        protected IonValue(bool isNull)
        {
            if (isNull)
            {
                NullFlagOn(true);
            }
        }

        /// <summary>
        /// Get this value's user type annotations.
        /// </summary>
        /// <returns>Read-only collection of type annotations.</returns>
        public IReadOnlyCollection<SymbolToken> GetTypeAnnotationSymbols()
        {
            if (_annotations == null)
                return SymbolToken.EmptyArray;

            return _annotations;
        }

        /// <summary>
        /// Add an annotation to this value.
        /// </summary>
        /// <param name="annotation">Annotation text.</param>
        public void AddTypeAnnotation(string annotation)
        {
            if (annotation == null)
            {
                AddTypeAnnotation(new SymbolToken(annotation, 0));
            }
            else
            {
                AddTypeAnnotation(new SymbolToken(annotation, SymbolToken.UnknownSid));
            }
        }

        /// <summary>
        /// Add an annotation to this value.
        /// </summary>
        /// <param name="annotation">Annotation symbol.</param>
        public void AddTypeAnnotation(SymbolToken annotation)
        {
            ThrowIfLocked();

            if (_annotations == null)
            {
                _annotations = new List<SymbolToken>(1);
            }

            _annotations.Add(annotation);
        }

        /// <summary>
        /// Clear all annotations of this value.
        /// </summary>
        public void ClearAnnotations()
        {
            ThrowIfLocked();
            if (_annotations == null)
            {
                _annotations = new List<SymbolToken>();
            }

            _annotations.Clear();
        }

        /// <summary>
        /// Returns true if the value contains such annotation.
        /// </summary>
        /// <param name="text">Annotation text.</param>
        public bool HasAnnotation(string text)
        {
            return _annotations != null &&
                (_annotations.Any(a => (a.Text == null && text == null) || text.Equals(a.Text)));
        }

        /// <summary>
        /// Get or set whether this value is a null value.
        /// </summary>
        public bool IsNull => NullFlagOn();

        /// <summary>
        /// Make this value become a null.
        /// </summary>
        public virtual void MakeNull()
        {
            ThrowIfLocked();
            NullFlagOn(true);
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
            if (other == null || Type() != other.Type())
                return false;

            var otherAnnotations = other._annotations;
            if (_annotations == null)
                return otherAnnotations == null || otherAnnotations.Count == 0;

            if (otherAnnotations == null || otherAnnotations.Count != _annotations.Count)
                return false;

            for (int i = 0, l = _annotations.Count; i < l; i++)
            {
                if (!_annotations[i].IsEquivalentTo(otherAnnotations[i]))
                    return false;
            }

            return true;
        }

        public void WriteTo(IIonWriter writer)
        {
            if (!(writer is IPrivateWriter privateWriter))
                throw new InvalidOperationException();

            if (writer.IsInStruct && !privateWriter.IsFieldNameSet())
            {
                if (FieldNameSymbol == default)
                    throw new IonException("Field name is not set");

                writer.SetFieldNameSymbol(FieldNameSymbol);
            }

            privateWriter.ClearTypeAnnotations();
            if (_annotations != null)
            {
                foreach (var a in _annotations)
                {
                    privateWriter.AddTypeAnnotationSymbol(a);
                }
            }

//            privateWriter.SetTypeAnnotation(GetTypeAnnotations());
            WriteBodyTo(privateWriter);
        }

        /// <summary>
        /// Concrete class implementations should call the correct writer method.
        /// </summary>
        internal abstract void WriteBodyTo(IPrivateWriter writer);

//        /// <summary>
//        /// Create a new instance of this Ion value with the same value but does not share the container context.
//        /// </summary>
//        /// <returns>A clone of this Ion value instance.</returns>
//        public IonValue Clone()
//        {
//            throw new NotImplementedException();
//        }

        private string GetErrorMessage()
        {
            return $"This operation is not supported for IonType {Type()}";
        }

        public bool IsReadOnly => LockedFlagOn();

        public void MakeReadOnly() => LockedFlagOn(true);

        // Applicable to IonDecimal
        public virtual decimal DecimalValue
        {
            get => throw new InvalidOperationException(GetErrorMessage());
        }
        public virtual BigDecimal BigDecimalValue
        {
            get => throw new InvalidOperationException(GetErrorMessage());
        }

        // Applicable to IonContainer
        public virtual int Count => throw new InvalidOperationException(GetErrorMessage());

        // Applicable to IonText
        public virtual string StringValue
        {
            get => throw new InvalidOperationException(GetErrorMessage());
        }

        // Applicable to IonInt
        public virtual IntegerSize IntegerSize => throw new InvalidOperationException(GetErrorMessage());

        public virtual BigInteger BigIntegerValue
        {
            get => throw new InvalidOperationException(GetErrorMessage());
        }

        public virtual int IntValue
        {
            get => throw new InvalidOperationException(GetErrorMessage());
        }

        public virtual long LongValue
        {
            get => throw new InvalidOperationException(GetErrorMessage());
        }

        // Applicable to IonSymbol
        public virtual SymbolToken SymbolValue
        {
            get => throw new InvalidOperationException(GetErrorMessage());
        }

        // Applicable to IonBool
        public virtual bool BoolValue
        {
            get => throw new InvalidOperationException(GetErrorMessage());
        }

        // Applicable to IonFloat
        public virtual double DoubleValue
        {
            get => throw new InvalidOperationException(GetErrorMessage());
        }

        // Applicable to IonTimestamp
        public virtual Timestamp TimestampValue
        {
            get => throw new InvalidOperationException(GetErrorMessage());
        }

        public string ToPrettyString()
        {
            using (var sw = new StringWriter())
            {
                var writer = new IonTextWriter(sw, new IonTextOptions {PrettyPrint = true});
                WriteTo(writer);
                writer.Finish();
                return sw.ToString();
            }
        }

        public virtual bool IsEquivalentTo(IIonValue value)
        {
            var valueIonValue = (IonValue)value;
            return IsEquivalentTo(valueIonValue);
        }

        public virtual ReadOnlySpan<byte> Bytes()
        {
            throw new InvalidOperationException(GetErrorMessage());
        }

        public virtual void SetBytes(ReadOnlySpan<byte> buffer)
        {
            throw new InvalidOperationException(GetErrorMessage());
        }

        public virtual int ByteSize()
        {
            throw new InvalidOperationException(GetErrorMessage());
        }

        public virtual StreamReader NewReader(Encoding encoding)
        {
            throw new InvalidOperationException(GetErrorMessage());
        }

        public virtual void RemoveAt(int index)
        {
            throw new InvalidOperationException(GetErrorMessage());
        }

        public virtual IIonValue GetElementAt(int index)
        {
            throw new InvalidOperationException(GetErrorMessage());
        }

        public virtual bool ContainsField(string fieldName)
        {
            throw new InvalidOperationException(GetErrorMessage());
        }

        public virtual IIonValue GetField(string fieldName)
        {
            throw new InvalidOperationException(GetErrorMessage());
        }

        public virtual void SetField(string fieldName, IIonValue value)
        {
            throw new InvalidOperationException(GetErrorMessage());
        }

        public virtual bool RemoveField(string fieldName)
        {
            throw new InvalidOperationException(GetErrorMessage());
        }

        public virtual void Clear()
        {
            throw new InvalidOperationException(GetErrorMessage());
        }

        public virtual void Add(IIonValue item)
        {
            throw new InvalidOperationException(GetErrorMessage());
        }

        public virtual int IndexOf(IIonValue item)
        {
            throw new InvalidOperationException(GetErrorMessage());
        }

        public virtual bool Remove(IIonValue item)
        {
            throw new InvalidOperationException(GetErrorMessage());
        }

        public virtual bool Contains(IIonValue item)
        {
            throw new InvalidOperationException(GetErrorMessage());
        }

        public virtual void CopyTo(IIonValue[] array, int arrayIndex)
        {
            throw new InvalidOperationException(GetErrorMessage());
        }

        public virtual IEnumerator<IIonValue> GetEnumerator()
        {
            throw new InvalidOperationException(GetErrorMessage());
        }

        public virtual IonType Type()
        {
            throw new InvalidOperationException("This operation is not supported for this IonType}");
        }
    }
}
