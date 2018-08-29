using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using IonDotnet.Utils;

namespace IonDotnet.Internals.Lite
{
    internal abstract class IonValueLite : IPrivateIonValue
    {
        private const int TypeAnnotationHashSignature = 620086508;

        private class LazySymbolTableProvider : ISymbolTableProvider
        {
            private ISymbolTable _symbolTable;
            private readonly IIonValue _ionValue;

            public LazySymbolTableProvider(IIonValue ionValue) => _ionValue = ionValue;

            public ISymbolTable GetSystemTable() => _symbolTable ?? (_symbolTable = _ionValue.SymbolTable);
        }

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

        /// <summary>
        /// This field stores information about different value properties, and element Id
        /// First byte for flags
        /// The rest 24bit for element id
        /// </summary>
        private uint _flags;

        private int _fieldId = SymbolToken.UnknownSid;
        private string _fieldName;

        /// <summary>
        /// The annotation sequence. This array is overallocated and may have nulls at the end denoting unused slots.
        /// </summary>
        private SymbolToken[] _annotations;

        private IContext _context;

        protected IonValueLite(ContainerlessContext containerlessContext, bool isNull)
        {
            _context = containerlessContext ?? throw new ArgumentNullException(nameof(containerlessContext));
            IsNullValue(isNull);
        }

        protected IonValueLite(IonValueLite existing, IContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            if (null != existing._annotations)
            {
                _annotations = new SymbolToken[existing._annotations.Length];
                for (int i = 0, s = _annotations.Length; i < s; i++)
                {
                    var existingToken = existing._annotations[i];
                    if (existingToken == SymbolToken.None) continue;

                    var text = existingToken.Text;
                    if (text != null)
                    {
                        _annotations[i] = new SymbolToken(text, SymbolToken.UnknownSid);
                    }
                    else
                    {
                        // TODO this is clearly wrong; however was the existing behavior as existing under #getAnnotationTypeSymbols();
                        _annotations[i] = existing._annotations[i];
                    }
                }
            }

            _flags = existing._flags;
            _context = context;
            ClearFlag(LockedFlag);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected uint GetMetadata(uint mask, int shift) => (_flags & mask) >> shift;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void SetMetadata(uint metadata, uint mask, int shift)
        {
            _flags &= ~mask;
            _flags |= ((metadata << shift) & mask);
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
        private bool IsLocked() => HasFlag(LockedFlag);

        private bool IsLocked(bool value)
        {
            if (value)
            {
                SetFlag(LockedFlag);
            }
            else
            {
                ClearFlag(LockedFlag);
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool IsBoolTrue() => HasFlag(BoolTrueFlag);

        protected bool IsBoolTrue(bool value)
        {
            if (value)
            {
                SetFlag(BoolTrueFlag);
            }
            else
            {
                ClearFlag(BoolTrueFlag);
            }

            return value;
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
        protected bool IsNullValue() => HasFlag(NullFlag);

        protected bool IsNullValue(bool value)
        {
            if (value)
            {
                SetFlag(NullFlag);
            }
            else
            {
                ClearFlag(NullFlag);
            }

            return value;
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

        protected IonSystemLite GetIonSystemLite() => _context.GetSystem();

        protected void CheckLocked()
        {
            if (IsLocked()) throw new InvalidOperationException("Value is locked");
        }

        protected abstract int GetHashCode(ISymbolTableProvider symbolTableProvider);
        public abstract IonValueLite Clone(IContext parentContext);
        protected abstract void WriteBodyTo(IIonWriter writer, ISymbolTableProvider symbolTableProvider);

        protected int HashTypeAnnotations(int original, ISymbolTableProvider symbolTableProvider)
        {
            var tokens = GetTypeAnnotationSymbols(symbolTableProvider);
            if (tokens.Length == 0)
            {
                return original;
            }

            const int sidHashSalt = 127; // prime to salt sid of annotation
            const int textHashSalt = 31; // prime to salt text of annotation
            const int prime = 8191;
            var result = prime * original + tokens.Length;

            foreach (var token in tokens)
            {
                var text = token.Text;
                var tokenHashCode = text?.GetHashCode() * textHashSalt ?? token.Sid * sidHashSalt;

                // mixing to account for small text and sid deltas
                tokenHashCode ^= (tokenHashCode << 19) ^ (tokenHashCode >> 13);
                result = prime * result + tokenHashCode;

                // mixing at each step to make the hash code order-dependent
                result ^= (result << 25) ^ (result >> 7);
            }

            return result;
        }

        private void WriteTo(IPrivateWriter writer, ISymbolTableProvider symbolTableProvider)
        {
            if (writer.IsInStruct && !writer.IsFieldNameSet())
            {
                var token = GetFieldNameSymbol(symbolTableProvider);
                if (token == SymbolToken.None) throw new InvalidOperationException("Fieldname not set");

                writer.SetFieldNameSymbol(token);
            }

            var annotations = GetTypeAnnotationSymbols();
            writer.SetTypeAnnotationSymbols(annotations);
            try
            {
                WriteBodyTo(writer, symbolTableProvider);
            }
            catch (IOException e)
            {
                throw new IonException(e);
            }
        }

        private void MakeReadOnlyPrivate()
        {
            ClearSymbolIds();
            IsLocked(true);
        }

        private void ClearSymbolIds()
        {
            if (_fieldName != null)
            {
                _fieldId = SymbolToken.UnknownSid;
            }

            if (_annotations == null) return;

            for (var i = 0; i < _annotations.Length; i++)
            {
                var a = _annotations[i];
                if (a.Text != null && a.Sid != SymbolToken.UnknownSid)
                {
                    _annotations[i] = new SymbolToken(a.Text, SymbolToken.UnknownSid);
                }
            }
        }

        protected void ThrowIfNull()
        {
            if (IsNullValue()) throw new NullValueException();
        }

        public void DetachFromContainer()
        {
            CheckLocked();

            ClearSymbolIds();
            _context = new ContainerlessContext(GetIonSystemLite());

            _fieldName = null;
            _fieldId = SymbolToken.UnknownSid;
            SetElementId(0);
        }

        public abstract IonType Type { get; }

        public bool IsNull => IsNullValue();

        public bool ReadOnly => IsLocked();

        public IIonSystem System => GetIonSystemLite();

        public int ElementId => GetElementId();

        public SymbolToken GetFieldNameSymbol(ISymbolTableProvider symbolTableProvider)
        {
            var sid = _fieldId;
            var text = _fieldName;
            if (text != null)
            {
                if (sid != SymbolToken.UnknownSid) return new SymbolToken(text, sid);

                var token = symbolTableProvider.GetSystemTable().Find(text);
                if (token != SymbolToken.None) return token;
            }
            else if (sid > 0)
            {
                text = symbolTableProvider.GetSystemTable().FindKnownSymbol(sid);
            }
            else if (sid != 0)
            {
                return SymbolToken.None;
            }

            return new SymbolToken(text, sid);
        }

        public ISymbolTable SymbolTable { get; set; }

        public ISymbolTable GetAssignedSymbolTable() => _context.GetContextSymbolTable();

        public SymbolToken[] GetTypeAnnotationSymbols(ISymbolTableProvider symbolTableProvider)
        {
            if (_annotations == null) return SymbolToken.EmptyArray;
            var count = _annotations.TakeWhile(a => a != SymbolToken.None).Count();
            if (count == 0) return SymbolToken.EmptyArray;

            // TODO do we need this in C#?
            for (var i = 0; i < count; i++)
            {
                var token = _annotations[i];
                if (token.Text == null || token.Sid != SymbolToken.UnknownSid) continue;

                var interned = symbolTableProvider.GetSystemTable().Find(token.Text);
                if (interned != SymbolToken.None)
                {
                    _annotations[i] = token;
                }
            }

            return _annotations;
        }

        public string FieldName
        {
            set => _fieldName = value;
            get
            {
                if (_fieldName != null) return _fieldName;
                if (_fieldId <= 0) return null;
                throw new UnknownSymbolException(_fieldId);
            }
        }

        public SymbolToken FieldNameSymbol
        {
            get => GetFieldNameSymbol(new LazySymbolTableProvider(this));
            set
            {
                Debug.Assert(_fieldId == SymbolToken.UnknownSid && _fieldName == null);
                _fieldId = value.Sid;
                _fieldName = value.Text;
            }
        }

        public IIonContainer Container => _context.GetContextContainer();

        public bool RemoveFromContainer()
        {
            CheckLocked();

            var parent = _context.GetContextContainer();
            return parent != null && parent.Remove(this);
        }

        public IIonValue TopLevelValue
        {
            get
            {
                if (this is IIonDatagram) throw new InvalidOperationException("Datagram is already top-level");

                var value = this;
                while (true)
                {
                    var container = value._context.GetContextContainer();
                    if (container == null || container is IIonDatagram) break;
                }

                return value;
            }
        }

        public string ToPrettyString()
        {
            throw new NotImplementedException();
        }

        public string[] GetTypeAnnotations()
        {
            var count = _annotations.TakeWhile(a => a != SymbolToken.None).Count();
            return PrivateHelper.ToTextArray(_annotations, count);
        }

        public ArraySegment<SymbolToken> GetTypeAnnotationSymbols() => new ArraySegment<SymbolToken>(_annotations);

        public bool HasTypeAnnotation(string annotation)
        {
            if (string.IsNullOrEmpty(annotation)) throw new ArgumentNullException(nameof(annotation));
            if (_annotations == null) return false;

            return Array.FindIndex(_annotations, a => a != SymbolToken.None && annotation == a.Text) >= 0;
        }

        public void SetTypeAnnotations(params string[] annotations)
        {
            throw new NotImplementedException();
        }

        public void SetTypeAnnotationSymbols(params SymbolToken[] annotations)
            => _annotations = (annotations == null || annotations.Length == 0) ? SymbolToken.EmptyArray : (SymbolToken[]) annotations.Clone();

        public void ClearTypeAnnotations()
        {
            throw new NotImplementedException();
        }

        public void AddTypeAnnotation(string annotation)
        {
            CheckLocked();

            if (string.IsNullOrEmpty(annotation)) throw new ArgumentNullException(nameof(annotation));
            if (HasTypeAnnotation(annotation)) return;

            var token = new SymbolToken(annotation, SymbolToken.UnknownSid);
            var oldLength = _annotations?.Length ?? 0;
            if (oldLength > 0)
            {
                for (var i = 0; i < oldLength; i++)
                {
                    Debug.Assert(_annotations != null);
                    if (_annotations[i] != SymbolToken.None) continue;
                    _annotations[i] = token;
                    return;
                }
            }

            //if we reach here, oldLength is not enough
            var newLength = oldLength == 0 ? 1 : oldLength * 2;
            // TODO consider using ArrayPool here
            Array.Resize(ref _annotations, newLength);
            _annotations[oldLength] = token;
        }

        public void RemoveTypeAnnotation(string annotation)
        {
            throw new NotImplementedException();
        }

        public void WriteTo(IIonWriter writer)
        {
            if (!(writer is IPrivateWriter privateWriter))
                throw new NotSupportedException($"{nameof(IonValueLite)} can only write to {nameof(IPrivateWriter)}");

            WriteTo(privateWriter, new LazySymbolTableProvider(this));
        }

        public abstract void Accept(IValueVisitor visitor);

        public void MakeReadOnly()
        {
            if (IsLocked()) return;
            MakeReadOnlyPrivate();
        }

        public SymbolToken GetKnownFieldNameSymbol()
        {
            var token = GetFieldNameSymbol();
            if (token.Text == null && token.Sid != 0)
            {
                throw new UnknownSymbolException(_fieldId);
            }

            return token;
        }

        public SymbolToken GetFieldNameSymbol()
        {
            // TODO amzn/ion-java#27 We should memoize the results of symtab lookups.
            // BUT: that could cause thread-safety problems for read-only values.
            // I think makeReadOnly should populate the tokens fully
            // so that we only need to lookup from mutable instances.
            // However, the current invariants on these fields are nonexistant so
            // I do not trust that its safe to alter them here.

            return GetFieldNameSymbol(new LazySymbolTableProvider(this));
        }

        public void SetContext(IContext context)
        {
            CheckLocked();
            ClearSymbolIds();
            _context = context;
        }

        public sealed override bool Equals(object obj)
        {
            if (obj == this) return true;
            if (obj is IIonValue other) return IonComparison.IonEquals(this, other);
            return false;
        }

        /// <summary>
        /// Sets the field name and ID based on a SymbolToken. Both parts of the SymbolToken are trusted!
        /// </summary>
        /// <param name="name">is not retained by this value, but both fields are copied.</param>
        public void SetFieldNameSymbol(SymbolToken name)
        {
        }

        public sealed override int GetHashCode() => GetHashCode(new LazySymbolTableProvider(this));
    }
}
