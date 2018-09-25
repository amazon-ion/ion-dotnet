using System;
using System.IO;
using System.Text.RegularExpressions;

namespace IonDotnet.Internals.Text
{
    /// <summary>
    /// The user-level text reader is resposible for recoginizing symbols and process symbol tables.
    /// </summary>
    internal class UserTextReader : SystemTextReader
    {
        private static readonly Regex IvmRegex = new Regex("^\\$ion_[0-9]+_[0-9]+$", RegexOptions.Compiled);

        private ISymbolTable _currentSymtab;
        private readonly ICatalog _catalog;

        public UserTextReader(string textForm, ICatalog catalog = null, IonType parentType = IonType.None)
            : this(new CharSequenceStream(textForm), catalog, parentType)
        {
        }

        public UserTextReader(Stream utf8Stream, ICatalog catalog = null, IonType parentType = IonType.None)
            : this(new Utf8ByteStream(utf8Stream), catalog, parentType)
        {
        }

        public UserTextReader(Stream utf8Stream, Span<byte> bytesRead, ICatalog catalog = null, IonType parentType = IonType.None)
            : this(new Utf8ByteStream(utf8Stream, bytesRead), catalog, parentType)
        {
        }

        private UserTextReader(TextStream textStream, ICatalog catalog, IonType parentType)
            : base(textStream, parentType)
        {
            _catalog = catalog;
            _currentSymtab = _systemSymbols;
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
                                _currentSymtab = ReaderLocalTable.ImportReaderTable(this, _catalog, true);
                                _hasNextCalled = false;
                            }

                            break;
                        case IonType.Symbol:
                            if (_annotations.Count == 0)
                            {
                                // $ion_1_0 is read as an IVM only if it is not annotated
                                var version = SymbolValue().Text;
                                if (version is null || !IvmRegex.IsMatch(version))
                                {
                                    break;
                                }

                                //new Ivm found, reset all symbol tables
                                if (SystemSymbols.Ion10 != version)
                                    throw new UnsupportedIonVersionException(version);

                                MoveNext();
                                _currentSymtab = _systemSymbols;
                                _hasNextCalled = false;
                            }

                            break;
                    }
                }
            }

            return !_eof;
        }

        public override ISymbolTable GetSymbolTable()
        {
            return _currentSymtab;
        }
    }
}
