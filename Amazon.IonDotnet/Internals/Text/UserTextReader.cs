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

namespace Amazon.IonDotnet.Internals.Text
{
    using System;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using Amazon.IonDotnet.Internals.Conversions;

    /// <summary>
    /// The user-level text reader is resposible for recoginizing symbols and process symbol tables.
    /// </summary>
    internal class UserTextReader : SystemTextReader
    {
        private static readonly Regex IvmRegex = new Regex("^\\$ion_[0-9]+_[0-9]+$", RegexOptions.Compiled);

        private readonly ICatalog catalog;
        private ISymbolTable currentSymtab;

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
            this.catalog = catalog;
            this.currentSymtab = this.systemSymbols;
        }

        public override ISymbolTable GetSymbolTable()
        {
            return this.currentSymtab;
        }

        protected override bool HasNext()
        {
            while (!this.hasNextCalled)
            {
                base.HasNext();

                if (this.valueType != IonType.None && !this.CurrentIsNull && this.GetContainerType() == IonType.Datagram)
                {
                    switch (this.valueType)
                    {
                        case IonType.Struct:
                            if (this.annotations.Count > 0 && this.annotations[0].Text == SystemSymbols.IonSymbolTable)
                            {
                                this.currentSymtab = ReaderLocalTable.ImportReaderTable(this, this.catalog, true);
                                this.hasNextCalled = false;
                            }

                            break;
                        case IonType.Symbol:
                            // $ion_1_0 is read as an IVM only if it is not annotated.
                            if (this.annotations.Count == 0)
                            {
                                var version = this.SymbolValue().Text;
                                if (version is null || !IvmRegex.IsMatch(version))
                                {
                                    break;
                                }

                                // New IVM found, reset all symbol tables.
                                if (version != SystemSymbols.Ion10)
                                {
                                    throw new UnsupportedIonVersionException(version);
                                }

                                this.MoveNext();

                                // From specs: only unquoted $ion_1_0 text can be interpreted as IVM semantics and
                                // cause the symbol tables to be reset.
                                if (this.valueVariant.AuthoritativeType == ScalarType.String && this.scanner.Token != TextConstants.TokenSymbolQuoted)
                                {
                                    this.currentSymtab = this.systemSymbols;
                                }

                                // Even if that's not the case we still skip the IVM.
                                this,hasNextCalled = false;
                            }

                            break;
                        default:
                            break;
                    }
                }
            }

            return !this.eof;
        }
    }
}
