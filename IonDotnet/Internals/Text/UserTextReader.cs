using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using IonDotnet.Internals.Conversions;

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

        public UserTextReader(string textForm, ICatalog catalog = null)
            : this(new CharSequenceStream(textForm), catalog)
        {
        }

        public UserTextReader(Stream stream, ICatalog catalog = null)
            : this(new UnicodeStream(stream, Encoding.UTF8), catalog)
        {
        }

        public UserTextReader(Stream stream, Encoding encoding, ICatalog catalog = null)
            : this(new UnicodeStream(stream, encoding), catalog)
        {
        }

        public UserTextReader(Stream stream, Encoding encoding, Span<byte> bytesRead, ICatalog catalog = null)
            : this(new UnicodeStream(stream, encoding, bytesRead), catalog)
        {
        }

        public UserTextReader(Stream stream, Span<byte> bytesRead, ICatalog catalog = null)
            : this(new UnicodeStream(stream, bytesRead), catalog)
        {
        }

        private UserTextReader(TextStream textStream, ICatalog catalog)
            : base(textStream)
        {
            _catalog = catalog;
            _currentSymtab = _systemSymbols;
        }

        protected override bool HasNext()
        {
            while (!_hasNextCalled)
            {
                base.HasNext();

                if (_valueType != null && !CurrentIsNull && GetContainerType() == IonType.Datagram)
                {
                    switch (_valueType)
                    {
                        case IonType w when w.Id == IonType.Struct.Id:
                            if (_annotations.Count > 0 && _annotations[0].Text == SystemSymbols.IonSymbolTable)
                            {
                                _currentSymtab = ReaderLocalTable.ImportReaderTable(this, _catalog, true);
                                _hasNextCalled = false;
                            }

                            break;
                        case IonType w when w.Id == IonType.Symbol.Id:
                            // $ion_1_0 is read as an IVM only if it is not annotated
                            if (_annotations.Count == 0)
                            {
                                var version = SymbolValue().Text;
                                if (version is null || !IvmRegex.IsMatch(version))
                                {
                                    break;
                                }

                                //new Ivm found, reset all symbol tables
                                if (SystemSymbols.Ion10 != version)
                                    throw new UnsupportedIonVersionException(version);

                                MoveNext();


                                // from specs: only unquoted $ion_1_0 text can be interpreted as ivm semantics and 
                                // cause the symbol tables to be reset.
                                if (_v.AuthoritativeType == ScalarType.String && _scanner.Token != TextConstants.TokenSymbolQuoted)
                                {
                                    _currentSymtab = _systemSymbols;
                                }

                                //even if that's not the case we still skip the ivm
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
