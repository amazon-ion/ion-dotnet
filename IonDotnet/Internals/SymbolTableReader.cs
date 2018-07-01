using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming

namespace IonDotnet.Internals
{
    internal class SymbolTableReader : IIonReader
    {
        private enum Op
        {
            NEXT,
            STEPOUT
        }

        private const int S_BOF = 0;
        private const int S_STRUCT = 1;
        private const int S_IN_STRUCT = 2;
        private const int S_NAME = 3;
        private const int S_VERSION = 4;
        private const int S_MAX_ID = 5;
        private const int S_IMPORT_LIST = 6;
        private const int S_IN_IMPORTS = 7;
        private const int S_IMPORT_STRUCT = 8;
        private const int S_IN_IMPORT_STRUCT = 9;
        private const int S_IMPORT_NAME = 10;
        private const int S_IMPORT_VERSION = 11;
        private const int S_IMPORT_MAX_ID = 12;
        private const int S_IMPORT_STRUCT_CLOSE = 13;
        private const int S_IMPORT_LIST_CLOSE = 14;
        private const int S_AFTER_IMPORT_LIST = 15;
        private const int S_SYMBOL_LIST = 16;
        private const int S_IN_SYMBOLS = 17;
        private const int S_SYMBOL = 18;
        private const int S_SYMBOL_LIST_CLOSE = 19;
        private const int S_STRUCT_CLOSE = 20;
        private const int S_EOF = 21;

        //TODO re-implement this with C# flag enum sometime
        private const int HAS_NAME = 0x01;
        private const int HAS_VERSION = 0x02;
        private const int HAS_MAX_ID = 0x04;
        private const int HAS_IMPORT_LIST = 0x08;
        private const int HAS_SYMBOL_LIST = 0x10;

        private readonly ISymbolTable _symbolTable;
        private readonly int _maxId;

        private int _currentState = S_BOF;
        private int _flags;
        private string _stringValue;
        private long _intValue;
        private ISymbolTable[] _importedTables;
        private IIterator<ISymbolTable> _importTablesIterator;
        private ISymbolTable _currentImportTable;
        private IIterator<string> _localSymbolsIterator;


        public SymbolTableReader(ISymbolTable symbolTable)
        {
            _symbolTable = symbolTable ?? throw new ArgumentNullException(nameof(symbolTable));

            lock (_symbolTable)
            {
                _maxId = _symbolTable.MaxId;
                _localSymbolsIterator = _symbolTable.IterateDeclaredSymbolNames();
            }

            if (!_symbolTable.IsLocal)
            {
                SetFlag(HAS_NAME, true);
                SetFlag(HAS_VERSION, true);
            }

            //what is this???
            if (_maxId > 0)
            {
                // FIXME: is this ever true?            SetFlag(HAS_MAX_ID, true);
            }

            _importedTables = _symbolTable.GetImportedTables();
            if (_importedTables != null && _importedTables.Length > 0)
            {
                SetFlag(HAS_IMPORT_LIST, true);
            }

            if (_symbolTable.GetImportedMaxId() < _maxId)
            {
                SetFlag(HAS_SYMBOL_LIST, true);
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// this computes the actual move to the next state and
        /// update the current read value accordingly
        /// </summary>
        public IonType Next()
        {
            if (!HasNext()) return IonType.None;
            int new_state;
            switch (_currentState)
            {
                default:
                    ThrowUnrecognizedState(_currentState);
                    new_state = -1;
                    break;
                case S_BOF:
                    new_state = S_STRUCT;
                    break;
                case S_STRUCT:
                    //jump to the end if next() is called at S_STRUCT
                    //call stepin() to get into the struct
                    new_state = S_EOF;
                    break;
                case S_IN_STRUCT:
                    new_state = StateFirstInStruct();
                    LoadStateData(new_state);
                    break;
                case S_NAME:
                    Debug.Assert(HasVersion());
                    new_state = S_VERSION;
                    LoadStateData(new_state);
                    break;
                case S_VERSION:
                    if (HasMaxId())
                    {
                        new_state = S_MAX_ID;
                        LoadStateData(new_state);
                        break;
                    }

                    new_state = StateFollowingMaxId();
                    break;
                case S_MAX_ID:
                    new_state = StateFollowingMaxId();
                    break;
                case S_IMPORT_LIST:
                    new_state = StateFollowingImportList(Op.NEXT);
                    break;
            }

            _currentState = new_state;
            return StateType(_currentState);
        }

        private int StateFollowingImportList(Op op)
        {
            throw new NotImplementedException();
        }

        public void StepIn()
        {
            throw new NotImplementedException();
        }

        public void StepOut()
        {
            throw new NotImplementedException();
        }

        public int CurrentDepth => StateDepth(_currentState);

        public ISymbolTable GetSymbolTable() => _symbolTable;

        public IonType GetCurrentType() => StateType(_currentState);

        public IntegerSize GetIntegerSize() => StateType(_currentState) == IonType.Int ? IntegerSize.Int : IntegerSize.None;

        public string GetFieldName()
        {
            throw new NotImplementedException();
        }

        public SymbolToken GetFieldNameSymbol()
        {
            throw new NotImplementedException();
        }

        public bool CurrentIsNull()
        {
            throw new NotImplementedException();
        }

        public bool IsInStruct()
        {
            throw new NotImplementedException();
        }

        public bool BoolValue()
        {
            throw new NotImplementedException();
        }

        public int IntValue()
        {
            throw new NotImplementedException();
        }

        public long LongValue()
        {
            throw new NotImplementedException();
        }

        public BigInteger BigIntegerValue()
        {
            throw new NotImplementedException();
        }

        public double DoubleValue()
        {
            throw new NotImplementedException();
        }

        public decimal DecimalValue()
        {
            throw new NotImplementedException();
        }

        public DateTime DateTimeValue()
        {
            throw new NotImplementedException();
        }

        public string StringValue()
        {
            throw new NotImplementedException();
        }

        public SymbolToken SymbolValue()
        {
            throw new NotImplementedException();
        }

        public int LobByteSize()
        {
            throw new NotImplementedException();
        }

        public byte[] NewByteArray()
        {
            throw new NotImplementedException();
        }

        public int GetBytes(ArraySegment<byte> buffer)
        {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetFlag(int flagBit, bool on)
        {
            if (on)
            {
                _flags |= flagBit;
            }
            else
            {
                _flags &= ~flagBit;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TestFlag(int flagBit) => (_flags & flagBit) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasName() => TestFlag(HAS_NAME);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasVersion() => TestFlag(HAS_VERSION);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasMaxId() => TestFlag(HAS_MAX_ID);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasImports() => TestFlag(HAS_IMPORT_LIST);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasLocalSymbols() => TestFlag(HAS_SYMBOL_LIST);

        /// <summary>
        /// this just tells us whether or not we have more value coming at our current scanning depth
        /// </summary>
        /// <returns></returns>
        private bool HasNext()
        {
            switch (_currentState)
            {
                default:
                    ThrowUnrecognizedState(_currentState);
                    return false;
                case S_BOF:
                    return true;
                case S_STRUCT:
                    //only on top-level value
                    return false;
                case S_IN_STRUCT:
                    return StateFirstInStruct() != S_STRUCT_CLOSE;
                case S_NAME:
                    return true;
                case S_VERSION:
                    if (HasMaxId()) return true;
                    return StateFollowingMaxId() != S_STRUCT_CLOSE;
                case S_IMPORT_LIST:
                    return HasLocalSymbols();
                case S_IN_IMPORTS:
                case S_IMPORT_STRUCT:
                    // we have more if there is
                    return _importTablesIterator.HasNext();
                case S_IN_IMPORT_STRUCT:
                case S_IMPORT_NAME:
                    // we always have a name and version
                    return true;
                case S_IMPORT_VERSION:
                    // we always have a max_id on imports
                    return true;
                case S_IMPORT_MAX_ID:
                case S_IMPORT_STRUCT_CLOSE:
                case S_IMPORT_LIST_CLOSE:
                    return false;
                case S_AFTER_IMPORT_LIST:
                    // locals are the only thing that might follow imports
                    return HasLocalSymbols();
                case S_SYMBOL_LIST:
                    // the symbol list is the last member, so it has no "next sibling"
                    // but ... just in case we put something after the local symbol list
                    Debug.Assert(StateFollowingLocalSymbols() == S_STRUCT_CLOSE);
                    return false;
                case S_IN_SYMBOLS:
                case S_SYMBOL:
                    if (_localSymbolsIterator.HasNext()) return true;
                    return false;
                case S_SYMBOL_LIST_CLOSE:
                case S_STRUCT_CLOSE:
                case S_EOF:
                    // these are all at the end of their respective containers
                    return false;
            }
        }

        /// <summary>
        /// Update stringValue and intValue
        /// </summary>
        private void LoadStateData(int newState)
        {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int StateFollowingLocalSymbols() => S_STRUCT_CLOSE;

        private int StateFollowingMaxId()
        {
            if (HasImports()) return S_IMPORT_LIST;
            if (HasLocalSymbols()) return S_SYMBOL_LIST;
            return S_STRUCT_CLOSE;
        }

        private int StateFirstInStruct()
        {
            if (HasName()) return S_NAME;
            if (HasMaxId()) return S_MAX_ID;
            if (HasImports()) return S_IMPORT_LIST;
            return HasLocalSymbols() ? S_SYMBOL_LIST : S_STRUCT_CLOSE;
        }

        private static string GetStateName(int state)
        {
            switch (state)
            {
                default: return $"UnrecognizedState:{state}";
                case S_BOF: return "S_BOF";
                case S_STRUCT: return "S_STRUCT";
                case S_IN_STRUCT: return "S_IN_STRUCT";
                case S_NAME: return "S_NAME";
                case S_VERSION: return "S_VERSION";
                case S_MAX_ID: return "S_MAX_ID";
                case S_IMPORT_LIST: return "S_IMPORT_LIST";
                case S_IN_IMPORTS: return "S_IN_IMPORTS";
                case S_IMPORT_STRUCT: return "S_IMPORT_STRUCT";
                case S_IN_IMPORT_STRUCT: return "S_IN_IMPORT_STRUCT";
                case S_IMPORT_NAME: return "S_IMPORT_NAME";
                case S_IMPORT_VERSION: return "S_IMPORT_VERSION";
                case S_IMPORT_MAX_ID: return "S_IMPORT_MAX_ID";
                case S_IMPORT_STRUCT_CLOSE: return "S_IMPORT_STRUCT_CLOSE";
                case S_IMPORT_LIST_CLOSE: return "S_IMPORT_LIST_CLOSE";
                case S_AFTER_IMPORT_LIST: return "S_AFTER_IMPORT_LIST";
                case S_SYMBOL_LIST: return "S_SYMBOL_LIST";
                case S_IN_SYMBOLS: return "S_IN_SYMBOLS";
                case S_SYMBOL: return "S_SYMBOL";
                case S_SYMBOL_LIST_CLOSE: return "S_SYMBOL_LIST_CLOSE";
                case S_STRUCT_CLOSE: return "S_STRUCT_CLOSE";
                case S_EOF: return "S_EOF";
            }
        }

        private static IonType StateType(int state)
        {
            switch (state)
            {
                case S_BOF: return IonType.None;
                case S_STRUCT: return IonType.Struct;
                case S_IN_STRUCT: return IonType.None;
                case S_NAME: return IonType.String;
                case S_VERSION: return IonType.Int;
                case S_MAX_ID: return IonType.Int;
                case S_IMPORT_LIST: return IonType.List;
                case S_IN_IMPORTS: return IonType.None;
                case S_IMPORT_STRUCT: return IonType.Struct;
                case S_IN_IMPORT_STRUCT: return IonType.None;
                case S_IMPORT_NAME: return IonType.String;
                case S_IMPORT_VERSION: return IonType.Int;
                case S_IMPORT_MAX_ID: return IonType.Int;
                case S_IMPORT_STRUCT_CLOSE: return IonType.None;
                case S_IMPORT_LIST_CLOSE: return IonType.None;
                case S_AFTER_IMPORT_LIST: return IonType.None;
                case S_SYMBOL_LIST: return IonType.List;
                case S_IN_SYMBOLS: return IonType.None;
                case S_SYMBOL: return IonType.String;
                case S_SYMBOL_LIST_CLOSE: return IonType.None;
                case S_STRUCT_CLOSE: return IonType.None;
                case S_EOF: return IonType.None;
                default:
                    ThrowUnrecognizedState(state);
                    return IonType.None;
            }
        }

        private static int StateDepth(int state)
        {
            switch (state)
            {
                case S_BOF: return 0;
                case S_STRUCT: return 0;
                case S_IN_STRUCT: return 1;
                case S_NAME: return 1;
                case S_VERSION: return 1;
                case S_MAX_ID: return 1;
                case S_IMPORT_LIST: return 1;
                case S_IN_IMPORTS: return 2;
                case S_IMPORT_STRUCT: return 2;
                case S_IN_IMPORT_STRUCT: return 3;
                case S_IMPORT_NAME: return 3;
                case S_IMPORT_VERSION: return 3;
                case S_IMPORT_MAX_ID: return 3;
                case S_IMPORT_STRUCT_CLOSE: return 3;
                case S_IMPORT_LIST_CLOSE: return 2;
                case S_AFTER_IMPORT_LIST: return 1;
                case S_SYMBOL_LIST: return 1;
                case S_IN_SYMBOLS: return 2;
                case S_SYMBOL: return 2;
                case S_SYMBOL_LIST_CLOSE: return 2;
                case S_STRUCT_CLOSE: return 1;
                case S_EOF: return 0;
                default:
                    ThrowUnrecognizedState(state);
                    return -1;
            }
        }

        private static void ThrowUnrecognizedState(int state)
            => throw new IonException($"SymbolTableReader is in an unrecognize state: {state}");
    }
}
