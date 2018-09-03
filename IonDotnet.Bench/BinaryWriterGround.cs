using System;
using System.IO;
using System.Linq;
using IonDotnet.Internals.Binary;

namespace IonDotnet.Bench
{
    // ReSharper disable once UnusedMember.Global
    public class BinaryWriterGround : IRunable
    {
        public void Run(string[] args)
        {
            var outputStream = new MemoryStream();

            var writer = new ManagedBinaryWriter(outputStream, new ISymbolTable[0]);
            writer.StepIn(IonType.Struct);

            writer.SetFieldName("yes");
            writer.WriteBool(true);

            writer.SetFieldName("strings");
            writer.WriteString("abcd def adsd dasdas tiếng việt  😂");

            writer.SetFieldName("number_struct");
            writer.StepIn(IonType.Struct);
            writer.SetFieldName("number");
            writer.WriteInt(int.MaxValue / 2);
            writer.StepOut();

            writer.SetFieldName("float_min");
            writer.WriteFloat(float.MinValue);

            writer.SetFieldName("float_rand");
            writer.WriteFloat(float.MaxValue / 2);

            writer.SetFieldName("double_max");
            writer.WriteFloat(double.MaxValue);

            writer.SetFieldName("double_rand");
            writer.WriteFloat(3.12345678901);

            writer.StepOut();
            writer.WriteInt(int.MaxValue);
            writer.FlushAsync().Wait();
            writer.Dispose();

            var bytes = outputStream.ToArray();
            Console.WriteLine(bytes.Length);

            Console.WriteLine(string.Join(" ", bytes.Select(b => $"{b:X2}")));

            var reader = new UserBinaryReader(new MemoryStream(bytes));

            reader.MoveNext();
            Console.WriteLine(reader.CurrentType);
            reader.StepIn();

            reader.MoveNext();
            Console.WriteLine(reader.CurrentFieldName);
            Console.WriteLine(reader.CurrentType);
            Console.WriteLine(reader.BoolValue());
            Console.WriteLine();

            reader.MoveNext();
            Console.WriteLine(reader.CurrentFieldName);
            Console.WriteLine(reader.CurrentType);
            Console.WriteLine(reader.StringValue());
            Console.WriteLine();

            reader.MoveNext();
            Console.WriteLine(reader.CurrentFieldName);
            Console.WriteLine(reader.CurrentType);
            reader.StepIn();
            reader.MoveNext();
            Console.WriteLine(reader.CurrentFieldName);
            Console.WriteLine(reader.CurrentType);
            Console.WriteLine(reader.IntValue());
            reader.StepOut();

            reader.MoveNext();
            Console.WriteLine(reader.CurrentFieldName);
            Console.WriteLine(reader.CurrentType);
            Console.WriteLine(reader.DoubleValue());
            Console.WriteLine();

            reader.MoveNext();
            Console.WriteLine(reader.CurrentFieldName);
            Console.WriteLine(reader.CurrentType);
            Console.WriteLine(reader.DoubleValue());
            Console.WriteLine();

            reader.MoveNext();
            Console.WriteLine(reader.CurrentFieldName);
            Console.WriteLine(reader.CurrentType);
            Console.WriteLine(reader.DoubleValue());
            Console.WriteLine();

            reader.MoveNext();
            Console.WriteLine(reader.CurrentFieldName);
            Console.WriteLine(reader.CurrentType);
            Console.WriteLine(reader.DoubleValue());
            Console.WriteLine();
        }
    }
}
