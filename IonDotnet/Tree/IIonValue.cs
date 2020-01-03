using System.Collections.Generic;

namespace IonDotnet.Tree
{
    public interface IIonValue : IIonNull, IIonBool, IIonInt, IIonFloat,
        IIonDecimal, IIonTimestamp, IIonSymbol, IIonString, IIonClob,
        IIonBlob, IIonList, IIonSexp, IIonStruct, IIonDatagram
    {
        SymbolToken FieldNameSymbol { get; set; }
        bool IsNull { get; }
        bool IsReadOnly { get; }
        void AddTypeAnnotation(string annotation);
        void AddTypeAnnotation(SymbolToken annotation);
        void ClearAnnotations();
        IReadOnlyCollection<SymbolToken> GetTypeAnnotations();
        bool HasAnnotation(string text);
        bool IsEquivalentTo(IIonValue value);
        void MakeNull();
        void MakeReadOnly();
        string ToPrettyString();
        IonType Type();
        void WriteTo(IIonWriter writer);
    }
}
