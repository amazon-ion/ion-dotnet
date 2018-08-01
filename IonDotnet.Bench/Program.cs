using System;
using System.Text;

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

            try
            {
                var instance = (IRunable) Activator.CreateInstance(thatclass);
                instance.Run(args);
            }
            catch (Exception e)
            {
                Console.WriteLine("catched");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}
