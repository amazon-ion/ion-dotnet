using System;
using System.Numerics;
using System.Text;

namespace IonDotnet.Bench
{
    internal class Program
    {
        private static readonly BigInteger TwoPow63 = BigInteger.Multiply((long)1 << 62, 2);

        public static void Main(string[] args)
        {
            unchecked
            {
                var longVal = long.MaxValue - 4;
                var ulongVal = (ulong)longVal;
                ulongVal += 10;
                longVal = (long)ulongVal;

                longVal = (longVal << 1) >> 1;
                var big = BigInteger.Add(TwoPow63, longVal);
                Console.WriteLine(big - ulongVal);
            }
        }
    }
}
