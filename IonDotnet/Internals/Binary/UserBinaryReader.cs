using System.Diagnostics;
using System.IO;
using IonDotnet.Conversions;

namespace IonDotnet.Internals.Binary
{
    /// <inheritdoc />
    /// <summary>
    /// This reader is used to read user datagram
    /// </summary>
    /// <remarks>Starts out as a system bin reader</remarks>
    internal sealed class UserBinaryReader : SystemBinaryReader
    {
        internal UserBinaryReader(Stream input, IScalarConverter scalarConverter = null)
            : base(input, scalarConverter)
        {
        }

        public override IonType MoveNext()
        {
            if (!HasNext()) return IonType.None;
            _moveNextNeeded = true;
            return _valueType;
        }

        protected override bool HasNext()
        {
            if (_eof || !_moveNextNeeded) return !_eof;

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
            if (CurrentDepth != 0 || _valueIsNull) return;
            Debug.Assert(_valueTid != IonConstants.TidTypedecl);

            if (_valueTid == IonConstants.TidSymbol)
            {
                // trying to read a symbol here
                // $ion_1_0 is read as an IVM only if it is not annotated
                // we already count the number of annotations
                if (_annotationCount != 0) return;

                LoadOnce();

                // just get it straight from the holder, no conversion needed
                var sid = _v.IntValue;
                if (sid != SystemSymbols.Ion10Sid) return;

                _symbolTable = SharedSymbolTable.GetSystem(1);
                //user don't need to see this symbol so continue here
                _moveNextNeeded = true;
            }
            else if (_valueTid == IonConstants.TidStruct)
            {
                //trying to read the local symboltable here
                if (_hasSymbolTableAnnotation)
                {
                    _symbolTable = LocalSymbolTable.Read(this, false);
                    //user don't need to read the localsymboltable so continue
                    _moveNextNeeded = true;
                }
            }
        }
    }
}
