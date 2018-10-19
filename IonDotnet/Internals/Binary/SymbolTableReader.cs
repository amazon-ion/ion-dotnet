using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using IonDotnet.Conversions;

// ReSharper disable InconsistentNaming

namespace IonDotnet.Internals.Binary
{
    /// <inheritdoc />
    /// <summary>
    /// Implements a state machine for reading a symbol table
    /// </summary>
    internal class SymbolTableReader : IIonReader
    {
        private enum Op
        {
            Next,
            StepOut
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
        private int _intValue;
        private readonly ISymbolTable[] _importedTables;
        private IIterator<ISymbolTable> _importTablesIterator;
        private ISymbolTable _currentImportTable;
        private readonly IIterator<string> _localSymbolsIterator;


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
                // FIXME: is this ever true?
                SetFlag(HAS_MAX_ID, true);
            }

            _importedTables = _symbolTable.GetImportedTables().ToArray();
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
        public IonType MoveNext()
        {
            if (!HasNext())
                return IonType.None;
            var newState = _currentState;
            switch (_currentState)
            {
                default:
                    ThrowUnrecognizedState(_currentState);
                    newState = -1;
                    break;
                case S_BOF:
                    newState = S_STRUCT;
                    break;
                case S_STRUCT:
                    //jump to the end if next() is called at S_STRUCT
                    //call stepin() to get into the struct
                    newState = S_EOF;
                    break;
                case S_IN_STRUCT:
                    newState = StateFirstInStruct();
                    LoadStateData(newState);
                    break;
                case S_NAME:
                    Debug.Assert(HasVersion());
                    newState = S_VERSION;
                    LoadStateData(newState);
                    break;
                case S_VERSION:
                    if (HasMaxId())
                    {
                        newState = S_MAX_ID;
                        LoadStateData(newState);
                        break;
                    }

                    newState = StateFollowingMaxId();
                    break;
                case S_MAX_ID:
                    newState = StateFollowingMaxId();
                    break;
                case S_IMPORT_LIST:
                    newState = StateFollowingImportList(Op.Next);
                    break;
                case S_IN_IMPORTS:
                case S_IMPORT_STRUCT:
                    // we only need to get the import list once, which we
                    // do as we step into the import list, so it should
                    // be waiting for us here.
                    Debug.Assert(_importTablesIterator != null);
                    newState = NextImport();
                    break;
                case S_IN_IMPORT_STRUCT:
                    // shared tables have to have a name
                    newState = S_IMPORT_NAME;
                    LoadStateData(newState);
                    break;
                case S_IMPORT_NAME:
                    // shared tables have to have a version
                    newState = S_IMPORT_VERSION;
                    LoadStateData(newState);
                    break;
                case S_IMPORT_VERSION:
                    // and they also always have a max id - so we set up
                    // for it
                    newState = S_IMPORT_MAX_ID;
                    LoadStateData(newState);
                    break;
                case S_IMPORT_MAX_ID:
                    newState = S_IMPORT_STRUCT_CLOSE;
                    break;
                case S_IMPORT_STRUCT_CLOSE:
                case S_IMPORT_LIST_CLOSE:
                    // no change here - we just bump up against this local eof
                    break;
                case S_AFTER_IMPORT_LIST:
                    Debug.Assert(_symbolTable.GetImportedMaxId() < _maxId);
                    newState = S_SYMBOL_LIST;
                    break;
                case S_SYMBOL_LIST:
                    Debug.Assert(_symbolTable.GetImportedMaxId() < _maxId);
                    newState = StateFollowingLocalSymbols();
                    break;
                case S_IN_SYMBOLS:
                // we have some symbols - so we'll set up to read them, which we *have* to do once and *need* to do only once.
                // since we only get into the symbol list if there are some symbols - our next state
                // is at the first symbol so we just fall through to and let the S_SYMBOL
                // state do it's thing (which it will do every time we move to the next symbol)             
                case S_SYMBOL:
                    Debug.Assert(_localSymbolsIterator != null);
                    if (_localSymbolsIterator.HasNext())
                    {
                        _stringValue = _localSymbolsIterator.Next();
                        // null means this symbol isn't defined
                        newState = S_SYMBOL;
                        break;
                    }

                    newState = S_SYMBOL_LIST_CLOSE;
                    break;
                case S_SYMBOL_LIST_CLOSE:
                    // no change here - we just bump up against this local eof
                    newState = S_SYMBOL_LIST_CLOSE;
                    break;
                case S_STRUCT_CLOSE:
                case S_EOF:
                    // no change here - we just bump up against this local eof
                    break;
            }

            _currentState = newState;
            return StateType(_currentState);
        }

        private int NextImport()
        {
            if (_importTablesIterator.HasNext())
            {
                _currentImportTable = _importTablesIterator.Next();
                return S_IMPORT_STRUCT;
            }

            // the import list is empty, so we jump to
            // the close list and null out our current
            _currentImportTable = null;
            return S_IMPORT_LIST_CLOSE;
        }

        private int StateFollowingImportList(Op op)
        {
            if (!HasLocalSymbols()) return S_STRUCT_CLOSE;
            return op == Op.Next ? S_SYMBOL_LIST : S_AFTER_IMPORT_LIST;
        }

        public void StepIn()
        {
            switch (_currentState)
            {
                case S_STRUCT:
                    _currentState = S_IN_STRUCT;
                    break;
                case S_IMPORT_LIST:
                    _importTablesIterator = new PeekIterator<ISymbolTable>(_importedTables);
                    _currentState = S_IN_IMPORTS;
                    break;
                case S_IMPORT_STRUCT:
                    Debug.Assert(_currentImportTable != null);
                    _currentState = S_IN_IMPORT_STRUCT;
                    break;
                case S_SYMBOL_LIST:
                    _currentState = S_IN_SYMBOLS;
                    break;
                default:
                    throw new InvalidOperationException("current value is not a container");
            }
        }

        public void StepOut()
        {
            int newState;

            switch (_currentState)
            {
                default:
                    throw new InvalidOperationException("current value is not in a container");
                case S_IN_STRUCT:
                case S_NAME:
                case S_VERSION:
                case S_MAX_ID:
                case S_IMPORT_LIST:
                case S_AFTER_IMPORT_LIST:
                case S_SYMBOL_LIST:
                case S_STRUCT_CLOSE:
                    // these are all top level so stepOut() ends up at the end of our data
                    newState = S_EOF;
                    break;
                case S_IN_IMPORTS:
                case S_IMPORT_STRUCT:
                case S_IMPORT_LIST_CLOSE:
                    // if we're outside a struct, and we're in the import list stepOut will be whatever follows the import list
                    // close and we're done with these
                    _currentImportTable = null;
                    _importTablesIterator.Dispose();
                    newState = StateFollowingImportList(Op.StepOut);
                    break;
                case S_IN_IMPORT_STRUCT:
                case S_IMPORT_NAME:
                case S_IMPORT_VERSION:
                case S_IMPORT_MAX_ID:
                case S_IMPORT_STRUCT_CLOSE:
                    // if there is a next import the next state will be its struct open
                    // otherwise next will be the list close
                    newState = _importTablesIterator.HasNext() ? S_IMPORT_STRUCT : S_IMPORT_LIST_CLOSE;
                    break;
                case S_IN_SYMBOLS:
                case S_SYMBOL:
                case S_SYMBOL_LIST_CLOSE:
                    // I think this is just S_EOF, but if we ever put anything after the symbol list this
                    // will need to be updated. And we're done with our local symbol references.
                    _stringValue = null;
                    _localSymbolsIterator.Dispose();
                    newState = StateFollowingLocalSymbols();
                    break;
            }

            _currentState = newState;
        }

        public int CurrentDepth => StateDepth(_currentState);

        public ISymbolTable GetSymbolTable() => _symbolTable;

        public IonType CurrentType => StateType(_currentState);

        public IntegerSize GetIntegerSize() => StateType(_currentState) == IonType.Int ? IntegerSize.Int : IntegerSize.Unknown;

        public string CurrentFieldName
        {
            get
            {
                switch (_currentState)
                {
                    case S_STRUCT:
                    case S_IN_STRUCT:
                    case S_IN_IMPORTS:
                    case S_IMPORT_STRUCT:
                    case S_IN_IMPORT_STRUCT:
                    case S_IMPORT_STRUCT_CLOSE:
                    case S_IMPORT_LIST_CLOSE:
                    case S_AFTER_IMPORT_LIST:
                    case S_IN_SYMBOLS:
                    case S_SYMBOL:
                    case S_SYMBOL_LIST_CLOSE:
                    case S_STRUCT_CLOSE:
                    case S_EOF:
                        return null;

                    case S_NAME:
                    case S_IMPORT_NAME:
                        return SystemSymbols.Name;

                    case S_VERSION:
                    case S_IMPORT_VERSION:
                        return SystemSymbols.Version;

                    case S_MAX_ID:
                    case S_IMPORT_MAX_ID:
                        return SystemSymbols.MaxId;

                    case S_IMPORT_LIST:
                        return SystemSymbols.Imports;

                    case S_SYMBOL_LIST:
                        return SystemSymbols.Symbols;

                    default:
                        throw new IonException($"Internal error: {nameof(SymbolTableReader)} is in an unrecognized state: {_currentState}");
                }
            }
        }

        public SymbolToken GetFieldNameSymbol()
        {
            switch (_currentState)
            {
                case S_STRUCT:
                case S_IN_STRUCT:
                case S_IN_IMPORTS:
                case S_IMPORT_STRUCT:
                case S_IN_IMPORT_STRUCT:
                case S_IMPORT_STRUCT_CLOSE:
                case S_IMPORT_LIST_CLOSE:
                case S_AFTER_IMPORT_LIST:
                case S_IN_SYMBOLS:
                case S_SYMBOL:
                case S_SYMBOL_LIST_CLOSE:
                case S_STRUCT_CLOSE:
                case S_EOF:
                    return SymbolToken.None;

                case S_NAME:
                case S_IMPORT_NAME:
                    return new SymbolToken(SystemSymbols.Name, SystemSymbols.NameSid);

                case S_VERSION:
                case S_IMPORT_VERSION:
                    return new SymbolToken(SystemSymbols.Version, SystemSymbols.VersionSid);

                case S_MAX_ID:
                case S_IMPORT_MAX_ID:
                    return new SymbolToken(SystemSymbols.MaxId, SystemSymbols.MaxIdSid);

                case S_IMPORT_LIST:
                    return new SymbolToken(SystemSymbols.Imports, SystemSymbols.ImportsSid);

                case S_SYMBOL_LIST:
                    return new SymbolToken(SystemSymbols.Symbols, SystemSymbols.SymbolsSid);

                default:
                    throw new IonException($"Internal error: {nameof(SymbolTableReader)} is in an unrecognized state: {_currentState}");
            }
        }

        public bool CurrentIsNull
        {
            get
            {
                switch (_currentState)
                {
                    case S_STRUCT:
                    case S_IN_STRUCT:
                    case S_NAME:
                    case S_VERSION:
                    case S_MAX_ID:
                    case S_IMPORT_LIST:
                    case S_IN_IMPORTS:
                    case S_IMPORT_STRUCT:
                    case S_IN_IMPORT_STRUCT:
                    case S_IMPORT_NAME:
                    case S_IMPORT_VERSION:
                    case S_IMPORT_MAX_ID:
                    case S_IN_SYMBOLS:
                    case S_SYMBOL:
                        // these values are either present and non-null
                        // or entirely absent (in which case they will
                        // have been skipped and we won't be in a state
                        // to return them).
                        return false;

                    case S_IMPORT_STRUCT_CLOSE:
                    case S_IMPORT_LIST_CLOSE:
                    case S_AFTER_IMPORT_LIST:
                    case S_SYMBOL_LIST:
                    case S_SYMBOL_LIST_CLOSE:
                    case S_STRUCT_CLOSE:
                    case S_EOF:
                        // here we're not really on a value, so we're not
                        // on a value that is a null - so false again.
                        return false;

                    default:
                        throw new IonException($"Internal error: UnifiedSymbolTableReader is in an unrecognized state: {_currentState}");
                }
            }
        }

        public bool IsInStruct
        {
            get
            {
                switch (_currentState)
                {
                    case S_STRUCT:
                    case S_IN_IMPORTS:
                    case S_IMPORT_STRUCT:
                    case S_IN_SYMBOLS:
                    case S_SYMBOL:
                        // these values are either not contained, or
                        // contained in a list. So we aren't in a
                        // struct if they're pending.
                        return false;

                    case S_IN_STRUCT:
                    case S_NAME:
                    case S_VERSION:
                    case S_MAX_ID:
                    case S_IMPORT_LIST:
                    case S_IN_IMPORT_STRUCT:
                    case S_IMPORT_NAME:
                    case S_IMPORT_VERSION:
                    case S_IMPORT_MAX_ID:
                    case S_AFTER_IMPORT_LIST:
                    case S_SYMBOL_LIST:
                        // the values above are all members
                        // of a struct, so we must be in a
                        // struct to have them pending
                        return true;

                    case S_IMPORT_STRUCT_CLOSE:
                    case S_STRUCT_CLOSE:
                        // if we're closing a struct we're in a struct
                        return true;

                    case S_IMPORT_LIST_CLOSE:
                    case S_SYMBOL_LIST_CLOSE:
                    case S_EOF:
                        // if we're closing a list we in a list, not a struct
                        // and EOF is not in a struct
                        return false;

                    default:
                        throw new IonException($"Internal error: UnifiedSymbolTableReader is in an unrecognized state: {_currentState}");
                }
            }
        }

        public bool BoolValue() => throw new InvalidOperationException("only valid if the value is a boolean");

        public int IntValue() => _intValue;

        public long LongValue() => _intValue;

        public BigInteger BigIntegerValue() => new BigInteger(_intValue);

        public double DoubleValue() => throw new InvalidOperationException("only valid if the value is a double");

        public BigDecimal DecimalValue() => throw new InvalidOperationException("only valid if the value is a decimal");

        public Timestamp TimestampValue() => throw new InvalidOperationException("only valid if the value is a DateTime");

        public string StringValue() => _stringValue;

        public SymbolToken SymbolValue() => throw new InvalidOperationException("only valid if the value is a Symbol");

        public int GetLobByteSize() => throw new InvalidOperationException($"only valid if the value is a Lob, not {StateType(_currentState)}");

        public byte[] NewByteArray() => throw new InvalidOperationException($"only valid if the value is a Lob, not {StateType(_currentState)}");

        public int GetBytes(Span<byte> buffer)
            => throw new InvalidOperationException($"only valid if the value is a Lob, not {StateType(_currentState)}");

        public IEnumerable<SymbolToken> GetTypeAnnotations()
        {
            yield break;
        }

        public bool TryConvertTo(Type targetType, IScalarConverter scalarConverter, out object result) => throw new IonException($"Not supported in symbol talbe");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetFlag(int flagBit, bool on)
        {
            if (on)
            {
                _flags |= flagBit;
                return;
            }

            _flags &= ~flagBit;
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
            switch (newState)
            {
                default:
                    throw new IonException($"{nameof(SymbolTableReader)} in state {GetStateName(newState)} has no state to load");
                case S_NAME:
                    Debug.Assert(HasName());
                    _stringValue = _symbolTable.Name;
                    Debug.Assert(_stringValue != null);
                    break;
                case S_VERSION:
                    _intValue = _symbolTable.Version;
                    Debug.Assert(_intValue != 0);
                    break;
                case S_MAX_ID:
                    _intValue = _maxId;
                    break;
                case S_IMPORT_LIST:
                case S_SYMBOL_LIST:
                    // no op to simplify the initial fields logic in next()
                    break;
                case S_IMPORT_NAME:
                    Debug.Assert(_currentImportTable != null);
                    _stringValue = _currentImportTable.Name;
                    break;
                case S_IMPORT_VERSION:
                    // shared tables have to have a version
                    _stringValue = null;
                    _intValue = _currentImportTable.Version;
                    break;
                case S_IMPORT_MAX_ID:
                    // and they also always have a max id - so we set up
                    // for it
                    _intValue = _currentImportTable.MaxId;
                    break;
            }
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

        public void Dispose()
        {
            _importTablesIterator?.Dispose();
            _localSymbolsIterator?.Dispose();
        }
    }
}
