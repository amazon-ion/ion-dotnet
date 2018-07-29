using System;
using System.Collections.Generic;
using IonDotnet.Conversions;
using IonDotnet.Internals;

namespace IonDotnet.Tests.Common
{
    public class SaveAnnotationsReaderRoutine : IReaderRoutine
    {
        public HashSet<string> Symbols { get; } = new HashSet<string>();

        public void OnValueStart()
        {
            Symbols.Clear();
        }

        public void OnValueEnd()
        {
        }

        public void OnAnnotation(in SymbolToken symbolToken)
        {
            Symbols.Add(symbolToken.Text);
        }

        public bool TryConvertTo<T>(out T result) => throw new InvalidOperationException("This just saves annotations");
    }
}
