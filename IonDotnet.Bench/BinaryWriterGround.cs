using System;
using System.IO;
using System.Linq;
using IonDotnet.Internals;

namespace IonDotnet.Bench
{
    public class BinaryWriterGround : IRunable
    {
        public void Run(ArraySegment<string> args)
        {
            var outputStream = new MemoryStream();
            var writer = new ManagedBinaryWriter(outputStream, new ISymbolTable[0]);
            writer.StepIn(IonType.Struct);
            writer.SetFieldName("a");
            writer.WriteBool(true);
            writer.StepOut();
            writer.Finish();
            var bytes = outputStream.ToArray();
            var format = string.Join(" ", bytes.Select(b => $"{b:X2}"));
            Console.WriteLine(format);
            
            var reader = new UserBinaryReader(new MemoryStream(bytes));
            reader.Next();
            reader.StepIn();
            reader.Next();
            Console.WriteLine(reader.CurrentFieldName);
            Console.WriteLine(reader.BoolValue());
            
        }
    }
}
