using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using IonDotnet.Internals;
using IonDotnet.Internals.Text;
using IonDotnet.Systems;

namespace IonDotnet.Tree
{
    public abstract class IonValue : IEquatable<IonValue>
    {
        #region Flags

        private const uint LockedFlag = 0x01;
        private const uint SystemValueFlag = 0x02;
        private const uint NullFlag = 0x04;
        private const uint BoolTrueFlag = 0x08;
        private const uint IvmFlag = 0x10;
        private const uint AutoCreatedFlag = 0x20;
        private const uint SymbolPresentFlag = 0x40;

        //mask first 8 bits, the rest 0s. lower 8 bits is flags, the rest 24bits is element id
        private const uint ElementMask = 0xff;
        private const int ElementShift = 8;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected uint GetMetadata(uint mask, int shift) => (_flags & mask) >> shift;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void SetMetadata(uint metadata, uint mask, int shift)
        {
            _flags &= ~mask;
            _flags |= (metadata << shift) & mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetElementId(int elementId)
        {
            _flags &= ElementMask;
            _flags |= (uint) elementId << ElementShift;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetElementId() => (int) (_flags >> ElementShift);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasFlag(uint flagBit) => (_flags & flagBit) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetFlag(uint flagBit) => _flags |= flagBit;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearFlag(uint flagBit) => _flags &= ~flagBit;

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
                throw new InvalidOperationException("Value is locked");
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
        private uint _flags;

        #endregion

        protected List<SymbolToken> _annotations;
        public SymbolToken _fieldName;

        protected IonValue(bool isNull)
        {
            if (isNull)
            {
                NullFlagOn(true);
            }
        }

        protected IonValue GetTopLevelValue()
        {
            var val = this;
            while (val.Container != null)
            {
                val = val.Container;
            }

            return val;
        }

        public virtual ISymbolTable GetSymbolTable()
        {
            var topLevel = GetTopLevelValue();
            if (!(topLevel is IonDatagram datagram))
                return null;

            return datagram.GetSymbolTable();
        }

        /// <summary>
        /// Gets the container of this value, or null if this is not part of one.
        /// </summary>
        public virtual IonValue Container { get; internal set; }

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

        public void AddTypeAnnotation(SymbolToken symbolToken)
        {
            ThrowIfLocked();

            if (_annotations == null)
            {
                _annotations = new List<SymbolToken>(1);
            }

            _annotations.Add(symbolToken);
        }

        public void ClearAnnotations()
        {
            ThrowIfLocked();
            _annotations.Clear();
        }

        public bool HasAnnotation(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            if (_annotations == null)
                return false;

            for (var i = 0; i < _annotations.Count; i++)
            {
                if (_annotations[i].Text == text)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Get or set whether this value is a null value.
        /// </summary>
        public virtual bool IsNull
        {
            get => NullFlagOn();
            set
            {
                ThrowIfLocked();
                NullFlagOn(value);
            }
        }

        public bool Equals(IonValue other)
        {
            throw new NotImplementedException();
        }

        public void WriteTo(IIonWriter writer)
        {
            if (!(writer is IPrivateWriter privateWriter))
                throw new InvalidOperationException();

            if (writer.IsInStruct && !privateWriter.IsFieldNameSet())
            {
                if (_fieldName.Text == null)
                    throw new IonException("Field name is not set");

                writer.SetFieldNameSymbol(_fieldName);
            }

            privateWriter.SetTypeAnnotationSymbols(GetTypeAnnotations());
            WriteBodyTo(privateWriter);
        }

        /// <summary>
        /// Concrete class implementations should call the correct writer method.
        /// </summary>
        internal abstract void WriteBodyTo(IPrivateWriter writer);

        public IonValue Clone()
        {
            throw new NotImplementedException();
        }

        public bool IsReadOnly => LockedFlagOn();

        public void MakeReadOnly() => LockedFlagOn(true);

        /// <value>The <see cref="IonType"/> of this value.</value>
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
