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

namespace Amazon.IonDotnet.Internals.Binary
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Numerics;
    using System.Runtime.CompilerServices;

    /// <inheritdoc />
    /// <summary>
    /// Implements a state machine for reading a symbol table.
    /// </summary>
    internal class SymbolTableReader : IIonReader
    {
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

        private readonly ISymbolTable symbolTable;
        private readonly int maxId;
        private readonly ISymbolTable[] importedTables;
        private readonly IEnumerator<string> localSymbolsEnumerator;

        private int currentState = S_BOF;
        private int flags;
        private string stringValue;
        private int intValue;
        private int importTablesIdx = -1;
        private ISymbolTable currentImportTable;

        public SymbolTableReader(ISymbolTable symbolTable)
        {
            this.symbolTable = symbolTable ?? throw new ArgumentNullException(nameof(symbolTable));

            lock (this.symbolTable)
            {
                this.maxId = this.symbolTable.MaxId;
                this.localSymbolsEnumerator = this.symbolTable.GetDeclaredSymbolNames().GetEnumerator();
            }

            if (!this.symbolTable.IsLocal)
            {
                this.SetFlag(HAS_NAME, true);
                this.SetFlag(HAS_VERSION, true);
            }

            if (this.maxId > 0)
            {
                this.SetFlag(HAS_MAX_ID, true);
            }

            this.importedTables = symbolTable.GetImportedTables().ToArray();
            if (this.importedTables != null && this.importedTables.Length > 0)
            {
                this.SetFlag(HAS_IMPORT_LIST, true);
            }

            if (this.symbolTable.GetImportedMaxId() < this.maxId)
            {
                this.SetFlag(HAS_SYMBOL_LIST, true);
            }
        }

        // TODO: Revisit Finalize
        ~SymbolTableReader()
        {
            this.localSymbolsEnumerator?.Dispose();
        }

        private enum Op
        {
            Next,
            StepOut,
        }

        public int CurrentDepth => StateDepth(this.currentState);

        public IonType CurrentType => StateType(this.currentState);

        public string CurrentFieldName
        {
            get
            {
                switch (this.currentState)
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
                        throw new IonException($"Internal error: {nameof(SymbolTableReader)} is in an unrecognized state: {this.currentState}");
                }
            }
        }

        public bool CurrentIsNull
        {
            get
            {
                switch (this.currentState)
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
                        // These values are either present and non-null or entirely absent
                        return false;
                    case S_IMPORT_STRUCT_CLOSE:
                    case S_IMPORT_LIST_CLOSE:
                    case S_AFTER_IMPORT_LIST:
                    case S_SYMBOL_LIST:
                    case S_SYMBOL_LIST_CLOSE:
                    case S_STRUCT_CLOSE:
                    case S_EOF:
                        // Here we're not really on a value, so we're not on a value that is a null
                        return false;
                    default:
                        throw new IonException($"Internal error: UnifiedSymbolTableReader is in an unrecognized state: {this.currentState}");
                }
            }
        }

        public bool IsInStruct
        {
            get
            {
                switch (this.currentState)
                {
                    case S_STRUCT:
                    case S_IN_IMPORTS:
                    case S_IMPORT_STRUCT:
                    case S_IN_SYMBOLS:
                    case S_SYMBOL:
                        // These values are either not contained, or contained in a list
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
                        // The values above are all members of a struct
                        return true;
                    case S_IMPORT_STRUCT_CLOSE:
                    case S_STRUCT_CLOSE:
                        // If we're closing a struct we're in a struct
                        return true;
                    case S_IMPORT_LIST_CLOSE:
                    case S_SYMBOL_LIST_CLOSE:
                    case S_EOF:
                        // If we're closing a list we in a list, not a struct
                        // and EOF is not in a struct
                        return false;
                    default:
                        throw new IonException($"Internal error: UnifiedSymbolTableReader is in an unrecognized state: {this.currentState}");
                }
            }
        }

        /// <inheritdoc/>
        /// <summary>
        /// Compute the actual move to the next state and
        /// update the current read value accordingly.
        /// </summary>
        public IonType MoveNext()
        {
            if (!this.HasNext())
            {
                return IonType.None;
            }

            var newState = this.currentState;
            switch (this.currentState)
            {
                default:
                    ThrowUnrecognizedState(this.currentState);
                    newState = -1;
                    break;
                case S_BOF:
                    newState = S_STRUCT;
                    break;
                case S_STRUCT:
                    newState = S_EOF;
                    break;
                case S_IN_STRUCT:
                    newState = this.StateFirstInStruct();
                    this.LoadStateData(newState);
                    break;
                case S_NAME:
                    Debug.Assert(this.HasVersion(), "No version found");
                    newState = S_VERSION;
                    this.LoadStateData(newState);
                    break;
                case S_VERSION:
                    if (this.HasMaxId())
                    {
                        newState = S_MAX_ID;
                        this.LoadStateData(newState);
                        break;
                    }

                    newState = this.StateFollowingMaxId();
                    break;
                case S_MAX_ID:
                    newState = this.StateFollowingMaxId();
                    break;
                case S_IMPORT_LIST:
                    newState = this.StateFollowingImportList(Op.Next);
                    break;
                case S_IN_IMPORTS:
                case S_IMPORT_STRUCT:
                    // We only need to get the import list once, which we
                    // do as we step into the import list, so it should
                    // be waiting for us here.
                    newState = this.NextImport();
                    break;
                case S_IN_IMPORT_STRUCT:
                    // Shared tables have to have a name
                    newState = S_IMPORT_NAME;
                    this.LoadStateData(newState);
                    break;
                case S_IMPORT_NAME:
                    // Shared tables have to have a version
                    newState = S_IMPORT_VERSION;
                    this.LoadStateData(newState);
                    break;
                case S_IMPORT_VERSION:
                    // And they also always have a max id, so we set up for it
                    newState = S_IMPORT_MAX_ID;
                    this.LoadStateData(newState);
                    break;
                case S_IMPORT_MAX_ID:
                    newState = S_IMPORT_STRUCT_CLOSE;
                    break;
                case S_IMPORT_STRUCT_CLOSE:
                case S_IMPORT_LIST_CLOSE:
                    // No change here - we just bump up against this local eof
                    break;
                case S_AFTER_IMPORT_LIST:
                    Debug.Assert(this.symbolTable.GetImportedMaxId() < this.maxId, "maxId exceeded");
                    newState = S_SYMBOL_LIST;
                    break;
                case S_SYMBOL_LIST:
                    Debug.Assert(this.symbolTable.GetImportedMaxId() < this.maxId, "maxId exceeded");
                    newState = StateFollowingLocalSymbols();
                    break;
                case S_IN_SYMBOLS:
                // We have some symbols - so we'll set up to read them, which we *have* to do once and *need* to do only once.
                // Since we only get into the symbol list if there are some symbols, our next state
                // is at the first symbol so we just fall through to and let the S_SYMBOL
                // state do it's thing (which it will do every time we move to the next symbol)
                case S_SYMBOL:
                    Debug.Assert(this.localSymbolsEnumerator != null, "localSymbolsEnumerator is null");
                    if (this.localSymbolsEnumerator.MoveNext())
                    {
                        this.stringValue = this.localSymbolsEnumerator.Current;

                        // null means this symbol isn't defined
                        newState = S_SYMBOL;
                        break;
                    }

                    newState = S_SYMBOL_LIST_CLOSE;
                    break;
                case S_SYMBOL_LIST_CLOSE:
                    // No change here - we just bump up against this local eof
                    newState = S_SYMBOL_LIST_CLOSE;
                    break;
                case S_STRUCT_CLOSE:
                case S_EOF:
                    // No change here - we just bump up against this local eof
                    break;
            }

            this.currentState = newState;
            return StateType(this.currentState);
        }

        public void StepIn()
        {
            switch (this.currentState)
            {
                case S_STRUCT:
                    this.currentState = S_IN_STRUCT;
                    break;
                case S_IMPORT_LIST:
                    this.currentState = S_IN_IMPORTS;
                    break;
                case S_IMPORT_STRUCT:
                    Debug.Assert(this.currentImportTable != null, "currentImportTable is null");
                    this.currentState = S_IN_IMPORT_STRUCT;
                    break;
                case S_SYMBOL_LIST:
                    this.currentState = S_IN_SYMBOLS;
                    break;
                default:
                    throw new InvalidOperationException("Current value is not a container");
            }
        }

        public void StepOut()
        {
            int newState;

            switch (this.currentState)
            {
                default:
                    throw new InvalidOperationException("Current value is not in a container");
                case S_IN_STRUCT:
                case S_NAME:
                case S_VERSION:
                case S_MAX_ID:
                case S_IMPORT_LIST:
                case S_AFTER_IMPORT_LIST:
                case S_SYMBOL_LIST:
                case S_STRUCT_CLOSE:
                    // These are all top level so StepOut() ends up at the end of our data
                    newState = S_EOF;
                    break;
                case S_IN_IMPORTS:
                case S_IMPORT_STRUCT:
                case S_IMPORT_LIST_CLOSE:
                    // If we're outside a struct, and we're in the import list StepOut() will be whatever follows the import list
                    // Close and we're done with these
                    this.currentImportTable = null;
                    newState = this.StateFollowingImportList(Op.StepOut);
                    break;
                case S_IN_IMPORT_STRUCT:
                case S_IMPORT_NAME:
                case S_IMPORT_VERSION:
                case S_IMPORT_MAX_ID:
                case S_IMPORT_STRUCT_CLOSE:
                    // If there is a next import the next state will be its struct open
                    // Otherwise next will be the list close
                    newState = this.importTablesIdx < this.importedTables.Length - 1 ? S_IMPORT_STRUCT : S_IMPORT_LIST_CLOSE;
                    break;
                case S_IN_SYMBOLS:
                case S_SYMBOL:
                case S_SYMBOL_LIST_CLOSE:
                    // Done with our local symbol references.
                    this.stringValue = null;
                    this.localSymbolsEnumerator.Dispose();
                    newState = StateFollowingLocalSymbols();
                    break;
            }

            this.currentState = newState;
        }

        public ISymbolTable GetSymbolTable() => this.symbolTable;

        public IntegerSize GetIntegerSize() => StateType(this.currentState) == IonType.Int ? IntegerSize.Int : IntegerSize.Unknown;

        public SymbolToken GetFieldNameSymbol()
        {
            switch (this.currentState)
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
                    throw new IonException($"Internal error: {nameof(SymbolTableReader)} is in an unrecognized state: {this.currentState}");
            }
        }

        public bool BoolValue() => throw new InvalidOperationException("Only valid if the value is a boolean");

        public int IntValue() => this.intValue;

        public long LongValue() => this.intValue;

        public BigInteger BigIntegerValue() => new BigInteger(this.intValue);

        public double DoubleValue() => throw new InvalidOperationException("Only valid if the value is a double");

        public BigDecimal DecimalValue() => throw new InvalidOperationException("Only valid if the value is a decimal");

        public Timestamp TimestampValue() => throw new InvalidOperationException("Only valid if the value is a DateTime");

        public string StringValue() => this.stringValue;

        public SymbolToken SymbolValue() => throw new InvalidOperationException("Only valid if the value is a Symbol");

        public int GetLobByteSize() => throw new InvalidOperationException($"Only valid if the value is a Lob, not {StateType(this.currentState)}");

        public byte[] NewByteArray() => throw new InvalidOperationException($"Only valid if the value is a Lob, not {StateType(this.currentState)}");

        public int GetBytes(Span<byte> buffer)
            => throw new InvalidOperationException($"Only valid if the value is a Lob, not {StateType(this.currentState)}");

        public string[] GetTypeAnnotations()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<SymbolToken> GetTypeAnnotationSymbols()
        {
            throw new NotImplementedException();
        }

        public bool HasAnnotation(string annotation)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Dispose SymbolTableReader.
        /// </summary>
        public void Dispose()
        {
            return;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int StateFollowingLocalSymbols() => S_STRUCT_CLOSE;

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

        private int NextImport()
        {
            if (this.importTablesIdx < this.importedTables.Length - 1)
            {
                this.currentImportTable = this.importedTables[++this.importTablesIdx];
                return S_IMPORT_STRUCT;
            }

            // The import list is empty, so we jump to the close list and null out our current
            this.currentImportTable = null;
            return S_IMPORT_LIST_CLOSE;
        }

        private int StateFollowingImportList(Op op)
        {
            if (!this.HasLocalSymbols())
            {
                return S_STRUCT_CLOSE;
            }

            return op == Op.Next ? S_SYMBOL_LIST : S_AFTER_IMPORT_LIST;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetFlag(int flagBit, bool on)
        {
            if (on)
            {
                this.flags |= flagBit;
                return;
            }

            this.flags &= ~flagBit;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TestFlag(int flagBit) => (this.flags & flagBit) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasName() => this.TestFlag(HAS_NAME);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasVersion() => this.TestFlag(HAS_VERSION);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasMaxId() => this.TestFlag(HAS_MAX_ID);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasImports() => this.TestFlag(HAS_IMPORT_LIST);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasLocalSymbols() => this.TestFlag(HAS_SYMBOL_LIST);

        /// <summary>
        /// Determine whether or not more values are coming at the current scanning depth.
        /// </summary>
        /// <returns>True if there are more values at the current depth.</returns>
        private bool HasNext()
        {
            switch (this.currentState)
            {
                default:
                    ThrowUnrecognizedState(this.currentState);
                    return false;
                case S_BOF:
                    return true;
                case S_STRUCT:
                    return false;
                case S_IN_STRUCT:
                    return this.StateFirstInStruct() != S_STRUCT_CLOSE;
                case S_NAME:
                    return true;
                case S_VERSION:
                    if (this.HasMaxId())
                    {
                        return true;
                    }

                    return this.StateFollowingMaxId() != S_STRUCT_CLOSE;
                case S_IMPORT_LIST:
                    return this.HasLocalSymbols();
                case S_IN_IMPORTS:
                case S_IMPORT_STRUCT:
                    // We have more if there is
                    return this.importTablesIdx < this.importedTables.Length - 1;
                case S_IN_IMPORT_STRUCT:
                case S_IMPORT_NAME:
                    // We always have a name and version
                    return true;
                case S_IMPORT_VERSION:
                    // We always have a max_id on imports
                    return true;
                case S_IMPORT_MAX_ID:
                case S_IMPORT_STRUCT_CLOSE:
                case S_IMPORT_LIST_CLOSE:
                    return false;
                case S_AFTER_IMPORT_LIST:
                    // LocalSymbols are the only thing that might follow imports
                    return this.HasLocalSymbols();
                case S_SYMBOL_LIST:
                    // The symbol list is the last member, so it has no "next sibling",
                    // but just in case we put something after the local symbol list
                    Debug.Assert(StateFollowingLocalSymbols() == S_STRUCT_CLOSE, "Symbol list is not the last member");
                    return false;
                case S_IN_SYMBOLS:
                case S_SYMBOL:
                    // Return true here, and MoveNext() will figure out whether we still have symbols
                    return true;
                case S_SYMBOL_LIST_CLOSE:
                case S_STRUCT_CLOSE:
                case S_EOF:
                    // These are all at the end of their respective containers
                    return false;
            }
        }

        /// <summary>
        /// Update stringValue and intValue.
        /// </summary>
        private void LoadStateData(int newState)
        {
            switch (newState)
            {
                default:
                    throw new IonException($"{nameof(SymbolTableReader)} in state {GetStateName(newState)} has no state to load");
                case S_NAME:
                    Debug.Assert(this.HasName(), "No name found");
                    this.stringValue = this.symbolTable.Name;
                    Debug.Assert(this.stringValue != null, "stringValue is null");
                    break;
                case S_VERSION:
                    this.intValue = this.symbolTable.Version;
                    Debug.Assert(this.intValue != 0, "intValue is 0");
                    break;
                case S_MAX_ID:
                    this.intValue = this.maxId;
                    break;
                case S_IMPORT_LIST:
                case S_SYMBOL_LIST:
                    // no op to simplify the initial fields logic in next()
                    break;
                case S_IMPORT_NAME:
                    Debug.Assert(this.currentImportTable != null, "currentImporTable is null");
                    this.stringValue = this.currentImportTable.Name;
                    break;
                case S_IMPORT_VERSION:
                    // shared tables have to have a version
                    this.stringValue = null;
                    this.intValue = this.currentImportTable.Version;
                    break;
                case S_IMPORT_MAX_ID:
                    // and they also always have a max id - so we set up
                    // for it
                    this.intValue = this.currentImportTable.MaxId;
                    break;
            }
        }

        private int StateFollowingMaxId()
        {
            if (this.HasImports())
            {
                return S_IMPORT_LIST;
            }

            if (this.HasLocalSymbols())
            {
                return S_SYMBOL_LIST;
            }

            return S_STRUCT_CLOSE;
        }

        private int StateFirstInStruct()
        {
            if (this.HasName())
            {
                return S_NAME;
            }

            if (this.HasImports())
            {
                return S_IMPORT_LIST;
            }

            return this.HasLocalSymbols() ? S_SYMBOL_LIST : S_STRUCT_CLOSE;
        }
    }
}
