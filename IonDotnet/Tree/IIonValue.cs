using System.Collections.Generic;
using IonDotnet.Tree.Impl;

namespace IonDotnet.Tree
{
    public interface IIonValue
    {
        IReadOnlyCollection<SymbolToken> GetTypeAnnotations();
        bool HasAnnotation(string text);
        void AddTypeAnnotation(string annotation);
        void AddTypeAnnotation(SymbolToken annotation);
        void ClearAnnotations();
        bool IsEquivalentTo(IonValue other);
        void MakeReadOnly();
        string ToPrettyString();
        void WriteTo(IIonWriter writer);

        SymbolToken FieldNameSymbol { get; }
        IonType Type { get; }
    }
}
