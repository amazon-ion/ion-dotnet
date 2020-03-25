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

namespace Amazon.IonDotnet.Internals
{
    using System;
    using System.Collections.Generic;
    using Amazon.IonDotnet.Utils;

    /// <inheritdoc />
    /// <summary>
    /// Base class for text and tree writer.
    /// </summary>
    internal abstract class IonSystemWriter : PrivateIonWriterBase
    {
        protected readonly List<SymbolToken> annotations = new List<SymbolToken>();
        protected readonly ReaderLocalTable symbolTable;

        private readonly ISymbolTable systemSymtab;
        private string fieldName;
        private int fieldNameSid = SymbolToken.UnknownSid;

        protected IonSystemWriter()
        {
            this.systemSymtab = SharedSymbolTable.GetSystem(1);
            this.symbolTable = new ReaderLocalTable(this.systemSymtab);
        }

        public override ISymbolTable SymbolTable => this.symbolTable;

        public override void SetFieldName(string name)
        {
            this.fieldName = name;
            this.fieldNameSid = SymbolToken.UnknownSid;
        }

        public override void SetFieldNameSymbol(SymbolToken symbol)
        {
            if (symbol.Text is null)
            {
                symbol = Symbols.Localize(this.symbolTable, symbol);
            }

            this.fieldName = symbol.Text;
            this.fieldNameSid = symbol.Sid;
        }

        public override void ClearTypeAnnotations() => this.annotations.Clear();

        public override void AddTypeAnnotation(string annotation)
        {
            if (annotation is null)
            {
                // treat this as the $0 symbol
                this.AddTypeAnnotationSymbol(new SymbolToken(null, 0));
                return;
            }

            this.AddTypeAnnotationSymbol(new SymbolToken(annotation, SymbolToken.UnknownSid));
        }

        public override void AddTypeAnnotationSymbol(SymbolToken annotation)
        {
            if (annotation.Text is null)
            {
                // no text, check if sid is sth we know
                annotation = Symbols.Localize(this.symbolTable, annotation);
            }

            if (annotation == default)
            {
                throw new UnknownSymbolException(annotation.Sid);
            }

            this.annotations.Add(annotation);
        }

        public override void SetTypeAnnotations(IEnumerable<string> annotations)
        {
            this.annotations.Clear();
            foreach (var annotation in annotations)
            {
                this.AddTypeAnnotationSymbol(new SymbolToken(annotation, SymbolToken.UnknownSid));
            }
        }

        public override bool IsFieldNameSet() => this.fieldName != null || this.fieldNameSid > 0;

        public override void WriteIonVersionMarker()
        {
            if (this.GetDepth() != 0)
            {
                throw new InvalidOperationException($"Cannot write Ivm at depth {this.GetDepth()}");
            }

            if (this.systemSymtab.IonVersionId != SystemSymbols.Ion10)
            {
                throw new UnsupportedIonVersionException(this.symbolTable.IonVersionId);
            }

            this.WriteIonVersionMarker(this.systemSymtab);
        }

        public override void WriteSymbol(string symbol)
        {
            if (symbol == SystemSymbols.Ion10 && this.GetDepth() == 0 && this.annotations.Count == 0)
            {
                this.WriteIonVersionMarker();
                return;
            }

            this.WriteSymbolAsIs(new SymbolToken(symbol, SymbolToken.UnknownSid));
        }

        public override void WriteSymbolToken(SymbolToken symbolToken)
        {
            if (symbolToken.Text == SystemSymbols.Ion10 && this.GetDepth() == 0 && this.annotations.Count == 0)
            {
                this.WriteIonVersionMarker();
                return;
            }

            this.WriteSymbolAsIs(symbolToken);
        }

        protected abstract void WriteSymbolAsIs(SymbolToken symbolToken);

        protected abstract void WriteIonVersionMarker(ISymbolTable systemSymtab);

        /// <summary>
        /// Assume that we have a field name text or sid set.
        /// </summary>
        /// <returns>Field name as <see cref="SymbolToken"/>.</returns>
        /// <exception cref="InvalidOperationException">When field name is not set.</exception>
        protected SymbolToken AssumeFieldNameSymbol()
        {
            if (this.fieldName == null && this.fieldNameSid < 0)
            {
                throw new InvalidOperationException("Field name is missing");
            }

            return new SymbolToken(this.fieldName, this.fieldNameSid);
        }

        protected void ClearFieldName()
        {
            this.fieldName = null;
            this.fieldNameSid = SymbolToken.UnknownSid;
        }
    }
}
