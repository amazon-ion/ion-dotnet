﻿/*
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

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Amazon.IonDotnet.Internals.Conversions;

namespace Amazon.IonDotnet.Internals.Text
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
            _currentSymtab = systemSymbols;
        }

        protected override bool HasNext()
        {
            while (!hasNextCalled)
            {
                base.HasNext();

                if (valueType != IonType.None && !CurrentIsNull && GetContainerType() == IonType.Datagram)
                {
                    switch (valueType)
                    {
                        case IonType.Struct:
                            if (annotations.Count > 0 && annotations[0].Text == SystemSymbols.IonSymbolTable)
                            {
                                _currentSymtab = ReaderLocalTable.ImportReaderTable(this, _catalog, true);
                                hasNextCalled = false;
                            }

                            break;
                        case IonType.Symbol:
                            // $ion_1_0 is read as an IVM only if it is not annotated.
                            if (annotations.Count == 0)
                            {
                                var version = SymbolValue().Text;
                                if (version is null || !IvmRegex.IsMatch(version))
                                {
                                    break;
                                }

                                // New IVM found, reset all symbol tables.
                                if (SystemSymbols.Ion10 != version)
                                {
                                    throw new UnsupportedIonVersionException(version);
                                }

                                MoveNext();


                                // From specs: only unquoted $ion_1_0 text can be interpreted as IVM semantics and 
                                // cause the symbol tables to be reset.
                                if (valueVariant.AuthoritativeType == ScalarType.String && scanner.Token != TextConstants.TokenSymbolQuoted)
                                {
                                    _currentSymtab = systemSymbols;
                                }

                                // Even if that's not the case we still skip the IVM.
                                hasNextCalled = false;
                            }

                            break;
                        default:
                            break;
                    }
                }
            }

            return !eof;
        }

        public override ISymbolTable GetSymbolTable()
        {
            return _currentSymtab;
        }
    }
}
