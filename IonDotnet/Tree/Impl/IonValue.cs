using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using IonDotnet.Internals;
using IonDotnet.Internals.Text;
using IonDotnet.Systems;
using IonDotnet.Utils;

namespace IonDotnet.Tree.Impl
{
    /// <summary>
    /// Represents a tree view into Ion data. Each <see cref="IonValue" /> is a node in the tree. These values are
    /// mutable and strictly hierarchical. 
    /// </summary>
    public abstract class IonValue : IIonValue
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
        public SymbolToken FieldNameSymbol { get; internal set; }

        protected IonValue(bool isNull)
        {
            if (isNull)
            {
                NullFlagOn(true);
            }
        }

        /// <summary>
        /// The container of this value, or null if this is not part of one.
        /// </summary>
        public virtual IonContainer Container { get; internal set; }

        /// <summary>
        /// Get this value's user type annotations.
        /// </summary>
        /// <returns>Read-only collection of type annotations.</returns>
        public IReadOnlyCollection<SymbolToken> GetTypeAnnotations()
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
            AddTypeAnnotation(new SymbolToken(annotation, SymbolToken.UnknownSid));
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
        /// <exception cref="ArgumentNullException">When text is null.</exception>
        public bool HasAnnotation(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            return _annotations != null && _annotations.Any(a => text.Equals(a.Text));
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
        public virtual bool IsEquivalentTo(IonValue other)
        {
            if (other == null || Type != other.Type)
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

        public bool IsReadOnly => LockedFlagOn();

        public void MakeReadOnly() => LockedFlagOn(true);

        /// <summary>The <see cref="IonType"/> of this value.</summary>
        public abstract IonType Type { get; }

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
    }
}
