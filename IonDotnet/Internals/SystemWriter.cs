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
    internal abstract class SystemWriter : PrivateIonWriterBase
    {        
        private string _fieldName;
        private int _fieldNameSid = SymbolToken.UnknownSid;
        private IonWriterBuilderBase.InitialIvmHandlingOption _ivmHandlingOption;
        protected readonly List<SymbolToken> _annotations = new List<SymbolToken>();

        protected ISymbolTable _symbolTable;
        private readonly ISymbolTable _systemSymtab;

        protected SystemWriter(IonWriterBuilderBase.InitialIvmHandlingOption ivmHandlingOption)
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

        public override void SetTypeAnnotationSymbols(IEnumerable<SymbolToken> annotations)
        {
            _annotations.Clear();
            foreach (var annotation in annotations)
            {
                var a = Symbols.Localize(_symbolTable, annotation);

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
            _ivmHandlingOption = IonWriterBuilderBase.InitialIvmHandlingOption.Default;
        }
        
        public override void WriteSymbol(string symbol)
        {
            if (SystemSymbols.Ion10 == symbol && GetDepth()==0 && _annotations.Count==0)
            {
                WriteIonVersionMarker();
                return;
            }
            
            WriteSymbolString(symbol);
        }

        protected abstract void WriteSymbolString(string value);

        /// <summary>
        /// Assume that we have a field name text or sid set
        /// </summary>
        /// <returns>Field name as <see cref="SymbolToken"/></returns>
        /// <exception cref="InvalidOperationException">When no field name is set</exception>
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
            _ivmHandlingOption = IonWriterBuilderBase.InitialIvmHandlingOption.Suppress;
        }
    }
}
