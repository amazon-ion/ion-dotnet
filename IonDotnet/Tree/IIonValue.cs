﻿using System.Collections.Generic;

namespace IonDotnet.Tree
{
    public interface IIonValue : IIonNull, IIonBool, IIonInt, IIonFloat,
        IIonDecimal, IIonTimestamp, IIonSymbol, IIonString, IIonClob,
        IIonBlob, IIonList, IIonSexp, IIonStruct, IIonDatagram
    {
        IReadOnlyCollection<SymbolToken> GetTypeAnnotations();
        SymbolToken FieldNameSymbol { get; set; }
        bool HasAnnotation(string text);
        void AddTypeAnnotation(string annotation);
        void AddTypeAnnotation(SymbolToken annotation);
        void ClearAnnotations();
        void MakeReadOnly();
        string ToPrettyString();
        bool IsEquivalentTo(IIonValue value);
        bool IsReadOnly { get; }
        void MakeNull();
        IonType Type();
    }
}
