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
        private const int Bof = 0;
        private const int Struct = 1;
        private const int InStruct = 2;
        private const int Name = 3;
        private const int Version = 4;
        private const int MaxId = 5;
        private const int ImportList = 6;
        private const int InImports = 7;
        private const int ImportStruct = 8;
        private const int InImportStruct = 9;
        private const int ImportName = 10;
        private const int ImportVersion = 11;
        private const int ImportMaxId = 12;
        private const int ImportStructClose = 13;
        private const int ImportListClose = 14;
        private const int AfterImportList = 15;
        private const int SymbolList = 16;
        private const int InSymbols = 17;
        private const int Symbol = 18;
        private const int SymbolListClose = 19;
        private const int StructClose = 20;
        private const int Eof = 21;

        private const int HasNameFlag = 0x01;
        private const int HasVersionFlag = 0x02;
        private const int HasMaxIdFlag = 0x04;
        private const int HasImportListFlag = 0x08;
        private const int HasSymbolListFlag = 0x10;

        private readonly ISymbolTable symbolTable;
        private readonly int maxId;
        private readonly ISymbolTable[] importedTables;
        private readonly IEnumerator<string> localSymbolsEnumerator;

        private int currentState = Bof;
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
                this.SetFlag(HasNameFlag, true);
                this.SetFlag(HasVersionFlag, true);
            }

            if (this.maxId > 0)
            {
                this.SetFlag(HasMaxIdFlag, true);
            }

            this.importedTables = symbolTable.GetImportedTables().ToArray();
            if (this.importedTables != null && this.importedTables.Length > 0)
            {
                this.SetFlag(HasImportListFlag, true);
            }

            if (this.symbolTable.GetImportedMaxId() < this.maxId)
            {
                this.SetFlag(HasSymbolListFlag, true);
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
                    case Struct:
                    case InStruct:
                    case InImports:
                    case ImportStruct:
                    case InImportStruct:
                    case ImportStructClose:
                    case ImportListClose:
                    case AfterImportList:
                    case InSymbols:
                    case Symbol:
                    case SymbolListClose:
                    case StructClose:
                    case Eof:
                        return null;

                    case Name:
                    case ImportName:
                        return SystemSymbols.Name;

                    case Version:
                    case ImportVersion:
                        return SystemSymbols.Version;

                    case MaxId:
                    case ImportMaxId:
                        return SystemSymbols.MaxId;

                    case ImportList:
                        return SystemSymbols.Imports;

                    case SymbolList:
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
                    case Struct:
                    case InStruct:
                    case Name:
                    case Version:
                    case MaxId:
                    case ImportList:
                    case InImports:
                    case ImportStruct:
                    case InImportStruct:
                    case ImportName:
                    case ImportVersion:
                    case ImportMaxId:
                    case InSymbols:
                    case Symbol:
                        // These values are either present and non-null or entirely absent
                        return false;
                    case ImportStructClose:
                    case ImportListClose:
                    case AfterImportList:
                    case SymbolList:
                    case SymbolListClose:
                    case StructClose:
                    case Eof:
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
                    case Struct:
                    case InImports:
                    case ImportStruct:
                    case InSymbols:
                    case Symbol:
                        // These values are either not contained, or contained in a list
                        return false;
                    case InStruct:
                    case Name:
                    case Version:
                    case MaxId:
                    case ImportList:
                    case InImportStruct:
                    case ImportName:
                    case ImportVersion:
                    case ImportMaxId:
                    case AfterImportList:
                    case SymbolList:
                        // The values above are all members of a struct
                        return true;
                    case ImportStructClose:
                    case StructClose:
                        // If we're closing a struct we're in a struct
                        return true;
                    case ImportListClose:
                    case SymbolListClose:
                    case Eof:
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
                case Bof:
                    newState = Struct;
                    break;
                case Struct:
                    newState = Eof;
                    break;
                case InStruct:
                    newState = this.StateFirstInStruct();
                    this.LoadStateData(newState);
                    break;
                case Name:
                    Debug.Assert(this.HasVersion(), "No version found");
                    newState = Version;
                    this.LoadStateData(newState);
                    break;
                case Version:
                    if (this.HasMaxId())
                    {
                        newState = MaxId;
                        this.LoadStateData(newState);
                        break;
                    }

                    newState = this.StateFollowingMaxId();
                    break;
                case MaxId:
                    newState = this.StateFollowingMaxId();
                    break;
                case ImportList:
                    newState = this.StateFollowingImportList(Op.Next);
                    break;
                case InImports:
                case ImportStruct:
                    // We only need to get the import list once, which we
                    // do as we step into the import list, so it should
                    // be waiting for us here.
                    newState = this.NextImport();
                    break;
                case InImportStruct:
                    // Shared tables have to have a name
                    newState = ImportName;
                    this.LoadStateData(newState);
                    break;
                case ImportName:
                    // Shared tables have to have a version
                    newState = ImportVersion;
                    this.LoadStateData(newState);
                    break;
                case ImportVersion:
                    // And they also always have a max id, so we set up for it
                    newState = ImportMaxId;
                    this.LoadStateData(newState);
                    break;
                case ImportMaxId:
                    newState = ImportStructClose;
                    break;
                case ImportStructClose:
                case ImportListClose:
                    // No change here - we just bump up against this local eof
                    break;
                case AfterImportList:
                    Debug.Assert(this.symbolTable.GetImportedMaxId() < this.maxId, "maxId exceeded");
                    newState = SymbolList;
                    break;
                case SymbolList:
                    Debug.Assert(this.symbolTable.GetImportedMaxId() < this.maxId, "maxId exceeded");
                    newState = StateFollowingLocalSymbols();
                    break;
                case InSymbols:
                // We have some symbols - so we'll set up to read them, which we *have* to do once and *need* to do only once.
                // Since we only get into the symbol list if there are some symbols, our next state
                // is at the first symbol so we just fall through to and let the S_SYMBOL
                // state do it's thing (which it will do every time we move to the next symbol)
                case Symbol:
                    Debug.Assert(this.localSymbolsEnumerator != null, "localSymbolsEnumerator is null");
                    if (this.localSymbolsEnumerator.MoveNext())
                    {
                        this.stringValue = this.localSymbolsEnumerator.Current;

                        // null means this symbol isn't defined
                        newState = Symbol;
                        break;
                    }

                    newState = SymbolListClose;
                    break;
                case SymbolListClose:
                    // No change here - we just bump up against this local eof
                    newState = SymbolListClose;
                    break;
                case StructClose:
                case Eof:
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
                case Struct:
                    this.currentState = InStruct;
                    break;
                case ImportList:
                    this.currentState = InImports;
                    break;
                case ImportStruct:
                    Debug.Assert(this.currentImportTable != null, "currentImportTable is null");
                    this.currentState = InImportStruct;
                    break;
                case SymbolList:
                    this.currentState = InSymbols;
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
                case InStruct:
                case Name:
                case Version:
                case MaxId:
                case ImportList:
                case AfterImportList:
                case SymbolList:
                case StructClose:
                    // These are all top level so StepOut() ends up at the end of our data
                    newState = Eof;
                    break;
                case InImports:
                case ImportStruct:
                case ImportListClose:
                    // If we're outside a struct, and we're in the import list StepOut() will be whatever follows the import list
                    // Close and we're done with these
                    this.currentImportTable = null;
                    newState = this.StateFollowingImportList(Op.StepOut);
                    break;
                case InImportStruct:
                case ImportName:
                case ImportVersion:
                case ImportMaxId:
                case ImportStructClose:
                    // If there is a next import the next state will be its struct open
                    // Otherwise next will be the list close
                    newState = this.importTablesIdx < this.importedTables.Length - 1 ? ImportStruct : ImportListClose;
                    break;
                case InSymbols:
                case Symbol:
                case SymbolListClose:
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
                case Struct:
                case InStruct:
                case InImports:
                case ImportStruct:
                case InImportStruct:
                case ImportStructClose:
                case ImportListClose:
                case AfterImportList:
                case InSymbols:
                case Symbol:
                case SymbolListClose:
                case StructClose:
                case Eof:
                    return SymbolToken.None;

                case Name:
                case ImportName:
                    return new SymbolToken(SystemSymbols.Name, SystemSymbols.NameSid);

                case Version:
                case ImportVersion:
                    return new SymbolToken(SystemSymbols.Version, SystemSymbols.VersionSid);

                case MaxId:
                case ImportMaxId:
                    return new SymbolToken(SystemSymbols.MaxId, SystemSymbols.MaxIdSid);

                case ImportList:
                    return new SymbolToken(SystemSymbols.Imports, SystemSymbols.ImportsSid);

                case SymbolList:
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
        private static int StateFollowingLocalSymbols() => StructClose;

        private static string GetStateName(int state)
        {
            switch (state)
            {
                default: return $"UnrecognizedState:{state}";
                case Bof: return "Bof";
                case Struct: return "Struct";
                case InStruct: return "InStruct";
                case Name: return "Name";
                case Version: return "Version";
                case MaxId: return "MaxId";
                case ImportList: return "ImportList";
                case InImports: return "InImports";
                case ImportStruct: return "ImportStruct";
                case InImportStruct: return "InImportStruct";
                case ImportName: return "ImportName";
                case ImportVersion: return "ImportVersion";
                case ImportMaxId: return "ImportMaxId";
                case ImportStructClose: return "ImportStructClose";
                case ImportListClose: return "ImportListClose";
                case AfterImportList: return "AfterImportList";
                case SymbolList: return "SymbolList";
                case InSymbols: return "InSymbols";
                case Symbol: return "Symbol";
                case SymbolListClose: return "SymbolListClose";
                case StructClose: return "StructClose";
                case Eof: return "Eof";
            }
        }

        private static IonType StateType(int state)
        {
            switch (state)
            {
                case Bof: return IonType.None;
                case Struct: return IonType.Struct;
                case InStruct: return IonType.None;
                case Name: return IonType.String;
                case Version: return IonType.Int;
                case MaxId: return IonType.Int;
                case ImportList: return IonType.List;
                case InImports: return IonType.None;
                case ImportStruct: return IonType.Struct;
                case InImportStruct: return IonType.None;
                case ImportName: return IonType.String;
                case ImportVersion: return IonType.Int;
                case ImportMaxId: return IonType.Int;
                case ImportStructClose: return IonType.None;
                case ImportListClose: return IonType.None;
                case AfterImportList: return IonType.None;
                case SymbolList: return IonType.List;
                case InSymbols: return IonType.None;
                case Symbol: return IonType.String;
                case SymbolListClose: return IonType.None;
                case StructClose: return IonType.None;
                case Eof: return IonType.None;
                default:
                    ThrowUnrecognizedState(state);
                    return IonType.None;
            }
        }

        private static int StateDepth(int state)
        {
            switch (state)
            {
                case Bof: return 0;
                case Struct: return 0;
                case InStruct: return 1;
                case Name: return 1;
                case Version: return 1;
                case MaxId: return 1;
                case ImportList: return 1;
                case InImports: return 2;
                case ImportStruct: return 2;
                case InImportStruct: return 3;
                case ImportName: return 3;
                case ImportVersion: return 3;
                case ImportMaxId: return 3;
                case ImportStructClose: return 3;
                case ImportListClose: return 2;
                case AfterImportList: return 1;
                case SymbolList: return 1;
                case InSymbols: return 2;
                case Symbol: return 2;
                case SymbolListClose: return 2;
                case StructClose: return 1;
                case Eof: return 0;
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
                return ImportStruct;
            }

            // The import list is empty, so we jump to the close list and null out our current
            this.currentImportTable = null;
            return ImportListClose;
        }

        private int StateFollowingImportList(Op op)
        {
            if (!this.HasLocalSymbols())
            {
                return StructClose;
            }

            return op == Op.Next ? SymbolList : AfterImportList;
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
        private bool HasName() => this.TestFlag(HasNameFlag);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasVersion() => this.TestFlag(HasVersionFlag);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasMaxId() => this.TestFlag(HasMaxIdFlag);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasImports() => this.TestFlag(HasImportListFlag);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasLocalSymbols() => this.TestFlag(HasSymbolListFlag);

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
                case Bof:
                    return true;
                case Struct:
                    return false;
                case InStruct:
                    return this.StateFirstInStruct() != StructClose;
                case Name:
                    return true;
                case Version:
                    if (this.HasMaxId())
                    {
                        return true;
                    }

                    return this.StateFollowingMaxId() != StructClose;
                case ImportList:
                    return this.HasLocalSymbols();
                case InImports:
                case ImportStruct:
                    // We have more if there is
                    return this.importTablesIdx < this.importedTables.Length - 1;
                case InImportStruct:
                case ImportName:
                    // We always have a name and version
                    return true;
                case ImportVersion:
                    // We always have a max_id on imports
                    return true;
                case ImportMaxId:
                case ImportStructClose:
                case ImportListClose:
                    return false;
                case AfterImportList:
                    // LocalSymbols are the only thing that might follow imports
                    return this.HasLocalSymbols();
                case SymbolList:
                    // The symbol list is the last member, so it has no "next sibling",
                    // but just in case we put something after the local symbol list
                    Debug.Assert(StateFollowingLocalSymbols() == StructClose, "Symbol list is not the last member");
                    return false;
                case InSymbols:
                case Symbol:
                    // Return true here, and MoveNext() will figure out whether we still have symbols
                    return true;
                case SymbolListClose:
                case StructClose:
                case Eof:
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
                case Name:
                    Debug.Assert(this.HasName(), "No name found");
                    this.stringValue = this.symbolTable.Name;
                    Debug.Assert(this.stringValue != null, "stringValue is null");
                    break;
                case Version:
                    this.intValue = this.symbolTable.Version;
                    Debug.Assert(this.intValue != 0, "intValue is 0");
                    break;
                case MaxId:
                    this.intValue = this.maxId;
                    break;
                case ImportList:
                case SymbolList:
                    // no op to simplify the initial fields logic in next()
                    break;
                case ImportName:
                    Debug.Assert(this.currentImportTable != null, "currentImporTable is null");
                    this.stringValue = this.currentImportTable.Name;
                    break;
                case ImportVersion:
                    // shared tables have to have a version
                    this.stringValue = null;
                    this.intValue = this.currentImportTable.Version;
                    break;
                case ImportMaxId:
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
                return ImportList;
            }

            if (this.HasLocalSymbols())
            {
                return SymbolList;
            }

            return StructClose;
        }

        private int StateFirstInStruct()
        {
            if (this.HasName())
            {
                return Name;
            }

            if (this.HasImports())
            {
                return ImportList;
            }

            return this.HasLocalSymbols() ? SymbolList : StructClose;
        }
    }
}
