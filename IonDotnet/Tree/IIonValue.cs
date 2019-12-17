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
        void MakeReadOnly();
        string ToPrettyString();
        void WriteTo(IIonWriter writer);
        bool IsEquivalentTo(IIonValue value);
        IonType Type { get; }
        bool IsReadOnly { get; }
        bool IsNull { get; }
        void MakeNull();
    }
}
