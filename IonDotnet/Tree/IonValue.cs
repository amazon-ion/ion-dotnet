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
        private byte _flags;

        /// <summary>
        /// Index to the the local symbol table in the datagram. Can point to up to 2^15 symtabs. 
        /// </summary>
        internal short _tableIndex = -1;

        #endregion

        protected List<SymbolToken> _annotations;

        /// <summary>
        /// Store the field name text and sid;
        /// </summary>
        internal string FieldName;

        protected IonValue(bool isNull)
        {
            if (isNull)
            {
                NullFlagOn(true);
            }
        }

        /// <summary>
        /// Get the 'top level' ancestor of this value in the value tree.
        /// The top level value is either parent-less or its parent is a <see cref="IonDatagram"/>.
        /// </summary>
        /// <returns>Top-level Ion value in the value tree.</returns>
        /// <remarks>This value is null for a <see cref="IonDatagram"/>.</remarks>
        protected IonValue GetTopLevelValue()
        {
            var val = this;
            while (!(val.Container is null) && !(val.Container is IonDatagram))
            {
                val = val.Container;
            }

            return val;
        }

        /// <summary>
        /// Any <see cref="IonValue"/> has a current symbol table which is the table used to decode this value. 
        /// </summary>
        /// <returns>The current symbol table of this value.</returns>
        public virtual ISymbolTable GetSymbolTable()
        {
            var topLevel = GetTopLevelValue();
            if (!(topLevel.Container is IonDatagram datagram))
                return null;

            return datagram.GetSymbolTableForChild(topLevel);
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
                if (FieldName == null)
                    throw new IonException("Field name is not set");

                writer.SetFieldName(FieldName);
            }

            privateWriter.SetTypeAnnotationSymbols(GetTypeAnnotations());
            WriteBodyTo(privateWriter);
        }


        /// <summary>
        /// Concrete class implementations should call the correct writer method.
        /// </summary>
        internal abstract void WriteBodyTo(IPrivateWriter writer);

        /// <summary>
        /// Create a new instance of this Ion value with the same value but does not share the container context.
        /// </summary>
        /// <returns>A clone of this Ion value instance.</returns>
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
