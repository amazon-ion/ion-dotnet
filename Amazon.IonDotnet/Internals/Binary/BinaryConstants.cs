/*
 * Copyright 2020 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

namespace Amazon.IonDotnet.Internals.Binary
{
    using System;
    using System.Runtime.CompilerServices;

    internal static class BinaryConstants
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

        public const int MaxAnnotationSize = 0x7F;

        internal const int ShortStringLength = 128;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetTypeCode(int tid) => tid >> 4;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetLowNibble(int tid) => tid & 0xf;

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
