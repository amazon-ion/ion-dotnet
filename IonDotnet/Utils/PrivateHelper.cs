namespace IonDotnet.Utils
{
    internal static class PrivateHelper
    {
        private static readonly string[] EmptyStringArray = new string[0];

        public static string[] ToTextArray(SymbolToken[] symbols, int count)
        {
            if (count == 0) return EmptyStringArray;
            var result = new string[count];
            for (var i = 0; i < count; i++)
            {
                var symbol = symbols[i];

                result[i] = symbol.Text ?? throw new UnknownSymbolException(symbol.Sid);
            }

            return result;
        }
    }
}
