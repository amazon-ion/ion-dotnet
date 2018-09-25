using System.Diagnostics;
using System.IO;
using IonDotnet.Conversions;

namespace IonDotnet.Internals.Binary
{
    /// <inheritdoc />
    /// <summary>
    /// This user-level reader is used to recognize symbols and process symbol table.
    /// </summary>
    /// <remarks>Starts out as a system bin reader</remarks>
    internal sealed class UserBinaryReader : SystemBinaryReader
    {
        private readonly ICatalog _catalog;

        internal UserBinaryReader(Stream input, IReaderRoutine readerRoutine = null, ICatalog catalog = null)
            : base(input, readerRoutine)
        {
            _catalog = catalog;
        }

        public override IonType MoveNext()
        {
            GetSymbolTable();
            if (!HasNext()) return IonType.None;
            _moveNextNeeded = true;
            return _valueType;
        }

        protected override bool HasNext()
        {
            if (_eof || !_moveNextNeeded)
                return !_eof;

            while (!_eof && _moveNextNeeded)
            {
                MoveNextUser();
            }

            return !_eof;
        }

        private void MoveNextUser()
        {
            base.HasNext();

            // if we're not at the top (datagram) level or the next value is null
            if (CurrentDepth != 0 || _valueIsNull) 
                return;
            Debug.Assert(_valueTid != BinaryConstants.TidTypedecl);

            if (_valueTid == BinaryConstants.TidSymbol)
            {
                // trying to read a symbol here
                // $ion_1_0 is read as an IVM only if it is not annotated
                // we already count the number of annotations
                if (_annotationCount != 0)
                    return;

                LoadOnce();

                // just get it straight from the holder, no conversion needed
                var sid = _v.IntValue;
                if (sid != SystemSymbols.Ion10Sid)
                    return;

                _symbolTable = SharedSymbolTable.GetSystem(1);
                //user don't need to see this symbol so continue here
                _moveNextNeeded = true;
            }
            else if (_valueTid == BinaryConstants.TidStruct)
            {
                //trying to read the local symboltable here
                if (_hasSymbolTableAnnotation)
                {
                    _symbolTable = ReaderLocalTable.ImportReaderTable(this, _catalog, false);
                    //user don't need to read the localsymboltable so continue
                    _moveNextNeeded = true;
                }
            }
        }
    }
}
