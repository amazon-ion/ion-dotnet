using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace IonDotnet.Utils
{
    internal static class IonComparison
    {
        internal const string UnknownSymbolTextPrefix = " -- UNKNOWN SYMBOL TEXT -- $";

        internal class Field
        {
            private readonly string _name;
            private readonly IIonValue _ionValue;
            private readonly bool _strict;

            public Field(IIonValue ionValue, bool strict)
            {
                _name = ionValue.FieldNameSymbol.Text ??
                        $"{UnknownSymbolTextPrefix}{ionValue.FieldNameSymbol.Sid}";

                _ionValue = ionValue;
                _strict = strict;
                Count = 0;
            }

            public int Count;

            public override int GetHashCode() => _name.GetHashCode();

            public override bool Equals(object obj)
            {
                Debug.Assert(obj != null, $"{nameof(obj)} != null");
                var other = (Field) obj;
                return _name.Equals(other._name) && IonEquals(_ionValue, other._ionValue, _strict);
            }
        }

        /// <summary>
        /// Checks for strict data equivalence over two Ion Values.
        /// </summary>
        /// <param name="v1">First value</param>
        /// <param name="v2">Second value</param>
        /// <returns>True if 2 values are equal</returns>
        public static bool IonEquals(IIonValue v1, IIonValue v2) => IonEquals(v1, v2, true);

        /// <summary>
        /// Checks for structural data equivalence over two Ion Values. That is, equivalence without considering any annotations.
        /// </summary>
        /// <param name="v1">First value</param>
        /// <param name="v2">Second value</param>
        /// <returns>True if 2 values are equal</returns>
        public static bool IonContentEquals(IIonValue v1, IIonValue v2) => IonEquals(v1, v2, false);

        private static int CompareAnnotations(Span<SymbolToken> ann1, Span<SymbolToken> ann2)
        {
            var len = ann1.Length;
            var result = len - ann2.Length;

            if (result != 0) return result;

            for (var i = 0; (result == 0) && (i < len); i++)
            {
                result = CompareSymbolTokens(ann1[i], ann2[i]);
            }

            return result;
        }

        private static int CompareSymbolTokens(SymbolToken tok1, SymbolToken tok2)
        {
            var text1 = tok1.Text;
            var text2 = tok2.Text;
            if (text1 != null && text2 != null) return string.Compare(text1, text2, StringComparison.Ordinal);

            if (text1 != null) return 1;
            if (text2 != null) return -1;

            return tok1.Sid.CompareTo(tok2.Sid);
        }

        private static IDictionary<Field, Field> ConvertToMultiset(IIonStruct ionStruct, bool strict)
        {
            var multiset = new Dictionary<Field, Field>();
            foreach (var structField in ionStruct)
            {
                var field = new Field(structField, strict);
                if (multiset.TryGetValue(field, out var existing))
                {
                    existing.Count++;
                }
                else
                {
                    multiset.Add(field, field);
                }
            }

            return multiset;
        }

        private static int CompareStructs(IIonStruct s1, IIonStruct s2, bool strict)
        {
            var result = s1.Size - s1.Size;
            if (result != 0) return result;

            var multiset = ConvertToMultiset(s1, strict);
            foreach (var s2Field in s2)
            {
                var field = new Field(s2Field, strict);
                if (!multiset.TryGetValue(field, out var mapped) || mapped.Count <= 0) return -1;
                mapped.Count--;
            }

            return result;
        }

        private static int CompareSequence(IIonSequence s1, IIonSequence s2, bool strict)
        {
            var result = s1.Count - s2.Count;
            if (result != 0) return result;

            var s2Enum = s2.GetEnumerator();
            foreach (var s1Field in s1)
            {
                if (!s2Enum.MoveNext())
                {
                    result = 1;
                    break;
                }

                result = IonCompare(s1Field, s2Enum.Current, strict);
                if (result != 0) break;
            }

            s2Enum.Dispose();
            return result;
        }

        private static int CompareLob(IIonLob lob1, IIonLob lob2)
        {
            var in1 = lob1.Size;
            var in2 = lob2.Size;
            var result = in1 - in2;
            if (result != 0) return result;

            using (var s1 = lob1.OpenInputStream())
            {
                using (var s2 = lob2.OpenInputStream())
                {
                    while (result == 0)
                    {
                        in1 = s1.ReadByte();
                        in2 = s2.ReadByte();
                        if (in1 == -1 && in2 == -1)
                        {
                            if (in1 != -1)
                                result = 1;
                            if (in2 != -1)
                                result = -1;
                            break;
                        }

                        result = (in1 - in2);
                    }
                }
            }

            return result;
        }

        private static bool IonEquals(IIonValue ionValue, IIonValue other, bool strict)
            => IonCompare(ionValue, other, strict) == 0;

        private static int IonCompare(IIonValue v1, IIonValue v2, bool strict)
        {
            if (v1 == null || v2 == null)
            {
                if (v1 == null && v2 == null) return 0;
                return v1 == null ? -1 : 1;
            }

            var result = v1.Type.CompareTo(v2.Type);
            if (result != 0) return result;

            var bo1 = v1.IsNull;
            var bo2 = v2.IsNull;
            if (bo1 || bo2)
            {
                if (!bo1)
                {
                    result = 1;
                }

                if (!bo2)
                {
                    result = -1;
                }
            }
            else
            {
                switch (v1.Type)
                {
                    case IonType.Null:
                        break;
                    case IonType.Bool:
                        result = ((IIonBool) v1).BooleanValue.CompareTo(((IIonBool) v2).BooleanValue);
                        break;
                    case IonType.Int:
                        result = ((IIonInt) v1).IntValue.CompareTo(((IIonInt) v2).IntValue);
                        break;
                    case IonType.Float:
                        result = ((IIonFloat) v1).FloatValue.CompareTo(((IIonFloat) v2).FloatValue);
                        break;
                    case IonType.Decimal:
                        result = ((IIonDecimal) v1).DecimalValue.CompareTo(((IIonDecimal) v2).DecimalValue);
                        break;
//                    case IonType.Timestamp:
//                        result = ((IIonTimestamp) v1).TimeStampValue.CompareTo(((IIonTimestamp) v2).TimeStampValue);
//                        break;
                    case IonType.String:
                        result = string.Compare(((IIonString) v1).StringValue, ((IIonString) v2).StringValue, StringComparison.Ordinal);
                        break;
                    case IonType.Symbol:
                        result = CompareSymbolTokens(((IIonSymbol) v1).SymbolValue, ((IIonSymbol) v2).SymbolValue);
                        break;
                    case IonType.Blob:
                    case IonType.Clob:
                        result = CompareLob(((IIonLob) v1), ((IIonLob) v2));
                        break;
                    case IonType.Struct:
                        result = CompareStructs(((IIonStruct) v1), ((IIonStruct) v2), strict);
                        break;
                    case IonType.List:
                    case IonType.Sexp:
                    case IonType.Datagram:
                        result = CompareSequence(((IIonSequence) v1), ((IIonSequence) v2), strict);
                        break;
                }
            }

            if (result == 0 && strict)
            {
                result = CompareAnnotations(v1.GetTypeAnnotationSymbols(), v2.GetTypeAnnotationSymbols());
            }

            return result;
        }
    }
}
