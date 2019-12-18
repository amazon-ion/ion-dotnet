using System.Collections.Generic;

namespace IonDotnet.Tree
{
    public interface IIonValue : IIonInt, IIonFloat, IIonTimestamp, IIonDecimal,
        IIonBlob, IIonClob, IIonDatagram, IIonList, IIonStruct,
        IIonNull, IIonSexp, IIonString, IIonSymbol, IIonBool
    {
        IReadOnlyCollection<SymbolToken> GetTypeAnnotations();
        bool HasAnnotation(string text);
        void AddTypeAnnotation(string annotation);
        void AddTypeAnnotation(SymbolToken annotation);
        void ClearAnnotations();
        void MakeReadOnly();
        string ToPrettyString();
        void WriteTo(IIonWriter writer);
        bool IsEquivalentTo(IIonValue value);
        bool IsReadOnly { get; }
        void MakeNull();
    }
}
