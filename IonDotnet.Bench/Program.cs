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
            var intVal = (int.MaxValue / 5);
            var ll = (float)intVal;
            Console.WriteLine(ll);
            //Console.WriteLine(Int32BitsToSingle(intVal));
            Console.WriteLine(BitConverter.Int32BitsToSingle(intVal));

            //long longVal = long.MaxValue / 12;
            //Console.WriteLine(longVal);

        }

        private static unsafe float Int32BitsToSingle(int value)
        {
           return *(float*)(&value);
        }
    }
}
