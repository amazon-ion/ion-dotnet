using System;
using System.IO;
using System.Numerics;
using IonDotnet.Internals;

namespace IonDotnet.Bench
{
    internal class Program
    {
        private static readonly BigInteger TwoPow63 = BigInteger.Multiply((long) 1 << 62, 2);

        public static void Main(string[] args)
        {
            var fs = new FileStream("javaout", FileMode.Open);
            var reader = new UserBinaryReader(fs, new DefaultScalarConverter());
            reader.StepIn();
            while ((reader.Next()) != IonType.None)
            {
                Console.WriteLine(reader.GetCurrentType());
            }
        }
    }
}
