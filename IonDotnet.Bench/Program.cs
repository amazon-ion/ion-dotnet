using System;

namespace IonDotnet.Bench
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine($"usage: <prog> <runner_classname>");
                Environment.Exit(1);
            }

            var className = args[0];
            var thatclass = Type.GetType($"IonDotnet.Bench.{className}");
            if (thatclass == null)
            {
                Console.Error.WriteLine($"Cannot find class {args[0]}");
                Environment.Exit(1);
            }

            if (thatclass.IsAssignableFrom(typeof(IRunable)))
            {
                Console.Error.WriteLine($"Class {args[0]} is not runable");
                Environment.Exit(1);
            }

            var instance = (IRunable) Activator.CreateInstance(thatclass);
            var argsSeg = args.Length > 1 ? new ArraySegment<string>(args, 1, args.Length - 1) : new ArraySegment<string>(new string[0]);
            instance.Run(argsSeg);
        }
    }
}
