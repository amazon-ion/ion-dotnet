using System;
using System.IO;
using System.Linq;
using IonDotnet.Internals.Binary;
using IonDotnet.Serialization;

namespace IonDotnet.Bench
{
    // ReSharper disable once UnusedMember.Global
    public class SerializerGround : IRunable
    {
        private class SimplePoco
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }

        public void Run(ArraySegment<string> args)
        {
            var serializer = new IonSerializer();
            var output = serializer.Serialize(new SimplePoco
            {
                Name = "huy",
                Age = 26
            });
            var s = string.Join(",", output.Select(b => $"0x{b:x2}"));
            Console.WriteLine(s);

            var reader = new UserBinaryReader(new MemoryStream(output));
            Console.WriteLine(reader.MoveNext());
            reader.StepIn();
            Console.WriteLine(reader.MoveNext());
            Console.WriteLine(reader.CurrentFieldName);
            Console.WriteLine(reader.StringValue());
            Console.WriteLine(reader.MoveNext());
            Console.WriteLine(reader.CurrentFieldName);
            Console.WriteLine(reader.IntValue());
        }
    }
}
