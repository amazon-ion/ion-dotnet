using System.Runtime.CompilerServices;

namespace IonDotnet.Internals
{
    internal static class IonConstants
    {
        public const int TidDatagram = 16; // not a real type id
        public const int TidNopPad = 99; // not a real type id

        public const int Eof = -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetTypeCode(int tid) => tid >> 4;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetLowNibble(int tid) => tid & 0xf;
    }
}
