using System;
using System.Collections.Generic;
using IonDotnet.Conversions;
using IonDotnet.Internals;

namespace IonDotnet.Tests.Common
{
    public class SaveAnnotationsConverter : IScalarConverter
    {
        public List<string> Symbols { get; } = new List<string>();

        public void OnValueStart()
        {
            Symbols.Clear();
        }

        public void OnValueEnd()
        {
        }

        public void OnSymbol(in SymbolToken symbolToken)
        {
            Symbols.Add(symbolToken.Text);
        }

        public T Convert<T>(in ValueVariant valueVariant) => throw new InvalidOperationException("This just saves annotations");
    }
}
