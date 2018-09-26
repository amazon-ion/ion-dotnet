using System;
using System.Collections.Generic;
using IonDotnet.Systems;
using IonDotnet.Utils;

namespace IonDotnet.Internals
{
    /// <inheritdoc />
    /// <summary>
    /// Base class for text and tree writer (why is this called 'system' ?) 
    /// </summary>
    internal abstract class IonSystemWriter : PrivateIonWriterBase
    {
        private string _fieldName;
        private int _fieldNameSid = SymbolToken.UnknownSid;
        protected IonWriterBuilderBase.InitialIvmHandlingOption _ivmHandlingOption;
        protected readonly List<SymbolToken> _annotations = new List<SymbolToken>();

        private readonly ISymbolTable _symbolTable;
        private readonly ISymbolTable _systemSymtab;

        protected IonSystemWriter(IonWriterBuilderBase.InitialIvmHandlingOption ivmHandlingOption)
        {
            _systemSymtab = SharedSymbolTable.GetSystem(1);
            _symbolTable = _systemSymtab;
            _ivmHandlingOption = ivmHandlingOption;
        }

        public override ISymbolTable SymbolTable => _symbolTable;

        public override void SetFieldName(string name)
        {
            _fieldName = name;
            _fieldNameSid = SymbolToken.UnknownSid;
        }

        public override void SetFieldNameSymbol(SymbolToken symbol)
        {
            _fieldName = symbol.Text;
            _fieldNameSid = symbol.Sid;
        }

        public override void AddTypeAnnotation(string annotation)
        {
            var symtok = _symbolTable.Find(annotation);
            if (symtok == default)
            {
                symtok = new SymbolToken(annotation, SymbolToken.UnknownSid);
            }

            _annotations.Add(symtok);
        }

        public override void SetTypeAnnotations(IEnumerable<string> annotations)
        {
            _annotations.Clear();
            foreach (var annotation in annotations)
            {
                var a = Symbols.Localize(_symbolTable, new SymbolToken(annotation, SymbolToken.UnknownSid));

                _annotations.Add(a);
            }
        }

        public override void SetTypeAnnotation(string annotation)
        {
            _annotations.Clear();
            AddTypeAnnotation(annotation);
        }

        public override bool IsFieldNameSet() => _fieldName != null || _fieldNameSid > 0;

        public override void WriteIonVersionMarker()
        {
            if (GetDepth() != 0)
                throw new InvalidOperationException($"Cannot write Ivm at depth {GetDepth()}");

            if (_systemSymtab.IonVersionId != SystemSymbols.Ion10)
                throw new UnsupportedIonVersionException(_symbolTable.IonVersionId);

            _ivmHandlingOption = IonWriterBuilderBase.InitialIvmHandlingOption.Default;
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
            if (symbolToken == default)
            {
                WriteNull(IonType.Symbol);
                return;
            }

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

        /// <summary>
        /// Called before writing a value
        /// </summary>
        protected virtual void StartValue()
        {
            if (_ivmHandlingOption == IonWriterBuilderBase.InitialIvmHandlingOption.Ensure)
            {
                WriteIonVersionMarker();
            }
        }

        protected void EndValue()
        {
            _ivmHandlingOption = IonWriterBuilderBase.InitialIvmHandlingOption.Default;
        }
    }
}
