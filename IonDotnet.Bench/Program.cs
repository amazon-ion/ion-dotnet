using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

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

//            const string emojis = "😀 😁 😂 🤣 😃 😄 😅 😆 😉";
//            var bytes = Encoding.UTF8.GetBytes(emojis);
//            var str = Encoding.UTF8.GetString(bytes);
//            Console.WriteLine(str == emojis);
//            Console.WriteLine(str);

//            BenchmarkRunner.Run<Benchmarks>();
//            var seg1 = new Block<char>(new[] {'1'});
//            var seg2 = new Block<char>(new[] {'2', '3'});
//            seg1.SetNext(seg2);
//            var seg = new ReadOnlySequence<char>(seg1, 0, seg2, 2);
//            Console.WriteLine(seg.Length);
//            foreach (var sm in seg)
//            {
//                for (var i = 0; i < sm.Length; i++)
//                {
//                    Console.WriteLine(sm.Span[i]);
//                }
//            }
        }
    }
}
