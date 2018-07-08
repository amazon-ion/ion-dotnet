using System;

namespace IonDotnet.Bench
{
    internal class Program
    {
        public static void Main(string[] args)
        {
//            var boolHashSignature = IonType.Bool.ToString().GetHashCode();
//            Console.WriteLine($"has sig {boolHashSignature}");
//
//            var trueHash = boolHashSignature ^ (unchecked(16777619 * 1231));
//
//            var falseHash = boolHashSignature ^ (unchecked(16777619 * 1237));
//
//            Console.WriteLine($"true {trueHash}");
//            Console.WriteLine($"false {falseHash}");
//
//            var nullHash = IonType.Null.ToString().GetHashCode();
//            Console.WriteLine($"nullHash sig {nullHash}");
//            Console.WriteLine("TYPE ANNOTATION".GetHashCode());

            var stringhash = IonType.String.ToString().GetHashCode();
            Console.WriteLine(stringhash);
            Console.WriteLine(IonType.String.ToString());
        }
    }
}
