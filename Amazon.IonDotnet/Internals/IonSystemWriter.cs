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

using System;
using System.Collections.Generic;
using Amazon.IonDotnet.Utils;
using Amazon.IonDotnet.Builders;

namespace Amazon.IonDotnet.Internals
{
    /// <inheritdoc />
    /// <summary>
    /// Base class for text and tree writer.
    /// </summary>
    internal abstract class IonSystemWriter : PrivateIonWriterBase
    {
        private string _fieldName;
        private int _fieldNameSid = SymbolToken.UnknownSid;
        protected readonly List<SymbolToken> _annotations = new List<SymbolToken>();

        protected readonly ReaderLocalTable _symbolTable;
        private readonly ISymbolTable _systemSymtab;

        protected IonSystemWriter()
        {
            _systemSymtab = SharedSymbolTable.GetSystem(1);
            _symbolTable = new ReaderLocalTable(_systemSymtab);
        }

        public override ISymbolTable SymbolTable => _symbolTable;

        public override void SetFieldName(string name)
        {
            _fieldName = name;
            _fieldNameSid = SymbolToken.UnknownSid;
        }

        public override void SetFieldNameSymbol(SymbolToken symbol)
        {
            if (symbol.Text is null)
            {
                symbol = Symbols.Localize(_symbolTable, symbol);
            }

            _fieldName = symbol.Text;
            _fieldNameSid = symbol.Sid;
        }

        public override void ClearTypeAnnotations() => _annotations.Clear();

        public override void AddTypeAnnotation(string annotation)
        {
            if (annotation is null)
            {
                //treat this as the $0 symbol
                AddTypeAnnotationSymbol(new SymbolToken(null, 0));
                return;
            }

            AddTypeAnnotationSymbol(new SymbolToken(annotation, SymbolToken.UnknownSid));
        }

        public override void AddTypeAnnotationSymbol(SymbolToken annotation)
        {
            if (annotation.Text is null)
            {
                //no text, check if sid is sth we know 
                annotation = Symbols.Localize(_symbolTable, annotation);
            }

            if (annotation == default)
            {
                throw new UnknownSymbolException(annotation.Sid);
            }
            _annotations.Add(annotation);
        }

        public override void SetTypeAnnotations(IEnumerable<string> annotations)
        {
            _annotations.Clear();
            foreach (var annotation in annotations)
            {
                AddTypeAnnotationSymbol(new SymbolToken(annotation, SymbolToken.UnknownSid));
            }
        }

        public override bool IsFieldNameSet() => _fieldName != null || _fieldNameSid > 0;

        public override void WriteIonVersionMarker()
        {
            if (GetDepth() != 0)
                throw new InvalidOperationException($"Cannot write Ivm at depth {GetDepth()}");

            if (_systemSymtab.IonVersionId != SystemSymbols.Ion10)
                throw new UnsupportedIonVersionException(_symbolTable.IonVersionId);

            WriteIonVersionMarker(_systemSymtab);
        }

        public override void WriteSymbol(string symbol)
        {
            if (SystemSymbols.Ion10 == symbol && GetDepth() == 0 && _annotations.Count == 0)
            {
                WriteIonVersionMarker();
                return;
            }

            WriteSymbolAsIs(new SymbolToken(symbol, SymbolToken.UnknownSid));
        }

        public override void WriteSymbolToken(SymbolToken symbolToken)
        {
            if (SystemSymbols.Ion10 == symbolToken.Text && GetDepth() == 0 && _annotations.Count == 0)
            {
                WriteIonVersionMarker();
                return;
            }

            WriteSymbolAsIs(symbolToken);
        }

        protected abstract void WriteSymbolAsIs(SymbolToken symbolToken);

        protected abstract void WriteIonVersionMarker(ISymbolTable systemSymtab);

        /// <summary>
        /// Assume that we have a field name text or sid set.
        /// </summary>
        /// <returns>Field name as <see cref="SymbolToken"/></returns>
        /// <exception cref="InvalidOperationException">When field name is not set.</exception>
        protected SymbolToken AssumeFieldNameSymbol()
        {
            if (_fieldName == null && _fieldNameSid < 0)
                throw new InvalidOperationException("Field name is missing");

            return new SymbolToken(_fieldName, _fieldNameSid);
        }

        protected void ClearFieldName()
        {
            _fieldName = null;
            _fieldNameSid = SymbolToken.UnknownSid;
        }
    }
}
