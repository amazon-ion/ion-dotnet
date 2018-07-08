using System;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using IonDotnet.Utils;

namespace IonDotnet.Internals.Lite
{
    internal abstract class IonValueLite : IPrivateIonValue
    {
        internal class LazySymbolTableProvider : ISymbolTableProvider
        {
            private ISymbolTable _symbolTable;
            private readonly IIonValue _ionValue;

            public LazySymbolTableProvider(IIonValue ionValue) => _ionValue = ionValue;

            public ISymbolTable GetSystemTable() => _symbolTable ?? (_symbolTable = _ionValue.SymbolTable);
        }

        protected const uint LockedFlag = 0x01;
        protected const uint SystemValueFlag = 0x02;
        protected const uint NullFlag = 0x04;
        protected const uint BoolTrueFlag = 0x08;
        protected const uint IvmFlag = 0x10;
        protected const uint AutoCreatedFlag = 0x20;
        protected const uint SymbolPresentFlag = 0x40;

        //mask first 8 bits, the rest 0s. lower 8 bits is flags, the rest 24bits is element id
        private const uint ElementMask = 0xff;
        protected const int ElementShift = 8;

        /// <summary>
        /// This field stores information about different value properties, and element Id
        /// First 6 bits for common props, bit 7and8 are for specific type to use
        /// The rest 24bit for element id
        /// </summary>
        private uint _flags;

        private int _fieldId = SymbolToken.UnknownSymbolId;
        private string _fieldName;
        protected IContext _context;

        protected IonValueLite(ContainerlessContext containerlessContext, bool isNull)
        {
            _context = containerlessContext ?? throw new ArgumentNullException(nameof(containerlessContext));
            IsNullValue(isNull);
        }

        protected IonValueLite(IonValueLite existing, IContext context)
        {
            // TODO annotation stuffs
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
        protected void SetElementId(int elementId)
        {
            _flags &= ElementMask;
            _flags |= (uint) elementId << ElementShift;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected int GetElementId() => (int) (_flags >> ElementShift);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasFlag(uint flagBit) => (_flags & flagBit) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetFlag(uint flagBit) => _flags |= flagBit;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearFlag(uint flagBit) => _flags &= ~flagBit;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool IsLocked() => HasFlag(LockedFlag);

        protected bool IsLocked(bool value)
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
            if (IsLocked()) throw new ReadOnlyException();
        }

        protected abstract int GetHashCode(ISymbolTableProvider symbolTableProvider);
        protected abstract IonValueLite Clone(IContext parentContext);

        public abstract IonType Type { get; }

        public bool IsNull => IsNullValue();

        public bool ReadOnly => IsLocked();

        public IIonSystem System => GetIonSystemLite();

        public int ElementId => GetElementId();

        public SymbolToken GetFieldNameSymbol(ISymbolTableProvider symbolTableProvider)
        {
            throw new NotImplementedException();
        }

        public ISymbolTable SymbolTable { get; set; }

        public ISymbolTable GetAssignedSymbolTable()
        {
            throw new NotImplementedException();
        }

        public string FieldName
        {
            get
            {
                if (_fieldName != null) return _fieldName;
                if (_fieldId <= 0) return null;
                throw new UnknownSymbolException(_fieldId);
            }
        }

        public SymbolToken FieldNameSymbol { get; }
        public IIonContainer Container => _context.GetContextContainer();

        public bool RemoveFromContainer()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public SymbolToken[] GetTypeAnnotationSymbols()
        {
            throw new NotImplementedException();
        }

        public bool HasTypeAnnotation(string annotation)
        {
            throw new NotImplementedException();
        }

        public void SetTypeAnnotations(params string[] annotations)
        {
            throw new NotImplementedException();
        }

        public void SetTypeAnnotationSymbols(params SymbolToken[] annotations)
        {
            throw new NotImplementedException();
        }

        public void ClearTypeAnnotations()
        {
            throw new NotImplementedException();
        }

        public void AddTypeAnnotation(string annotation)
        {
            throw new NotImplementedException();
        }

        public void RemoveTypeAnnotation(string annotation)
        {
            throw new NotImplementedException();
        }

        public void WriteTo(IIonWriter writer)
        {
            throw new NotImplementedException();
        }

        public abstract void Accept(IValueVisitor visitor);

        public void MakeReadOnly()
        {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj)
        {
            if (obj == this) return true;
            if (obj is IIonValue other) return IonComparison.IonEquals(this, other);
            return false;
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}
