using System;
using System.Runtime.CompilerServices;

namespace IonDotnet.Internals.Binary
{
    public static class BinaryConstants
    {
        public const int BinaryVersionMarkerLength = 4;

        public const int Eof = -1;

        public const int TidNull = 0;
        public const int TidBoolean = 1;
        public const int TidPosInt = 2;
        public const int TidNegInt = 3;
        public const int TidFloat = 4;
        public const int TidDecimal = 5;
        public const int TidTimestamp = 6;
        public const int TidSymbol = 7;
        public const int TidString = 8;
        public const int TidClob = 9;
        public const int TidBlob = 10; // a
        public const int TidList = 11; // b
        public const int TidSexp = 12; // c
        public const int TidStruct = 13; // d
        public const int TidTypedecl = 14; // e
        public const int TidUnused = 15; // f
        public const int TidDatagram = 16; // not a real type id
        public const int TidNopPad = 99; // not a real type id

        // TODO unify these
        public const int LnIsNull = 0x0f;

        public const int LnIsEmptyContainer = 0x00;
        public const int LnIsOrderedStruct = 0x01;
        public const int LnIsVarLen = 0x0e;

        public const int LnBooleanTrue = 0x01;
        public const int LnBooleanFalse = 0x00;
        public const int LnNumericZero = 0x00;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetTypeCode(int tid) => tid >> 4;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetLowNibble(int tid) => tid & 0xf;

        internal const int ShortStringLength = 128;

        public const int MaxAnnotationSize = 0x7F;

        public static byte GetNullByte(IonType type)
        {
            byte data;
            switch (type)
            {
                case IonType.None:
                case IonType.Null:
                    data = 0x0F;
                    break;
                case IonType.Bool:
                    data = 0x1F;
                    break;
                case IonType.Int:
                    data = 0x2F;
                    break;
                case IonType.Float:
                    data = 0x4F;
                    break;
                case IonType.Decimal:
                    data = 0x5F;
                    break;
                case IonType.Timestamp:
                    data = 0x6F;
                    break;
                case IonType.Symbol:
                    data = 0x7F;
                    break;
                case IonType.String:
                    data = 0x8F;
                    break;
                case IonType.Clob:
                    data = 0x9F;
                    break;
                case IonType.Blob:
                    data = 0xAF;
                    break;
                case IonType.List:
                    data = 0xBF;
                    break;
                case IonType.Sexp:
                    data = 0xCF;
                    break;
                case IonType.Struct:
                    data = 0xDF;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            return data;
        }
    }
}
