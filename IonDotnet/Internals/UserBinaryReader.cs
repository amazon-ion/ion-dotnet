using System.Diagnostics;
using System.IO;

namespace IonDotnet.Internals
{
    /// <summary>
    /// The user binary reader.
    /// </summary>
    /// <remarks>Starts out as a system bin reader</remarks>
    internal sealed class UserBinaryReader : SystemBinaryReader
    {
        internal UserBinaryReader(Stream input, IScalarConverter scalarConverter) : base(input, scalarConverter)
        {

        }

        private void HasNextUser()
        {
            HasNext();

            // if we're not at the top (datagram) level or the next value is null
            if (CurrentDepth != 0 || _valueIsNull) return;
            Debug.Assert(_valueTid != IonConstants.TidTypedecl);

            if (_valueTid == IonConstants.TidSymbol)
            {
                // trying to read a symbol here
                // $ion_1_0 is read as an IVM only if it is not annotated
                if (LoadAnnotations() != 0) return;

                if (!_v.IsEmpty)
                {
                    LoadOnce();
                }
                // just get it straight from the holder, no conversion needed
                var sid = _v.IntValue;
                if (sid != SystemSymbols.Ion10Sid) return;
                
                _symbolTable = SharedSymbolTable.GetSystem(1);
                _hasNextNeeded = true;
            }
            else if (_valueTid == IonConstants.TidStruct)
            {
                
            }
        }
    }
}
