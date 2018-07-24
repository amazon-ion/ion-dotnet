using System;
using System.IO;
using System.Linq;
using IonDotnet.Internals;
using IonDotnet.Internals.Binary;

namespace IonDotnet.Bench
{
    // ReSharper disable once UnusedMember.Global
    public class BinaryWriterGround : IRunable
    {
        public void Run(ArraySegment<string> args)
        {
            var outputStream = new MemoryStream();
            var writer = new ManagedBinaryWriter(outputStream, new ISymbolTable[0]);
            writer.StepIn(IonType.Struct);

            writer.SetFieldName("yes");
            writer.WriteBool(true);

            writer.SetFieldName("strings");
            writer.WriteString("this is what we want 😂");

            writer.SetFieldName("number");
            writer.WriteInt(323);

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
            Console.WriteLine(reader.CurrentType);
            Console.WriteLine(reader.BoolValue());
            Console.WriteLine();

            reader.Next();
            Console.WriteLine(reader.CurrentFieldName);
            Console.WriteLine(reader.CurrentType);
            Console.WriteLine(reader.StringValue());
            Console.WriteLine();
            
            reader.Next();
            Console.WriteLine(reader.CurrentFieldName);
            Console.WriteLine(reader.CurrentType);
            Console.WriteLine(reader.IntValue());
            Console.WriteLine();
        }
    }
}
