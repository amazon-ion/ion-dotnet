using System.Collections.Generic;
using IonDotnet.Conversions;

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
    }
}
