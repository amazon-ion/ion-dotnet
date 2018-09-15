using System;
using System.IO;
using System.Text.RegularExpressions;

namespace IonDotnet.Internals.Text
{
    internal class UserTextReader : SystemTextReader
    {
        private static readonly Regex IvmRegex = new Regex("^\\$ion_[0-9]+_[0-9]+$", RegexOptions.Compiled);

        public UserTextReader(string textForm, IonType parentType = IonType.None)
            : base(new CharSequenceStream(textForm), parentType)
        {
        }

        public UserTextReader(Stream utf8Stream, IonType parentType = IonType.None)
            : base(new Utf8ByteStream(utf8Stream), parentType)
        {
        }

        public UserTextReader(Stream utf8Stream, Span<byte> bytesRead, IonType parentType = IonType.None)
            : base(new Utf8ByteStream(utf8Stream, bytesRead), parentType)
        {
        }

        protected override bool HasNext()
        {
            while (!_hasNextCalled)
            {
                base.HasNext();

                if (_valueType != IonType.None && !CurrentIsNull && GetContainerType() == IonType.Datagram)
                {
                    switch (_valueType)
                    {
                        case IonType.Struct:
                            if (_annotations.Count > 0 && _annotations[0].Text == SystemSymbols.IonSymbolTable)
                            {
                                //TODO push new symbol local table
                            }

                            break;
                        case IonType.Symbol:
                            if (_annotations.Count == 0)
                            {
                                // $ion_1_0 is read as an IVM only if it is not annotated
                                var version = SymbolValue().Text;
                                if (IvmRegex.IsMatch(version))
                                {
                                    if (SystemSymbols.Ion10 != version)
                                        throw new UnsupportedIonVersionException(version);

                                    _hasNextCalled = false;
                                }
                            }

                            break;
                    }
                }
            }

            return !_eof;
        }
    }
}
