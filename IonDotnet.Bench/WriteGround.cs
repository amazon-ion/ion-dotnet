using System;
using System.IO;
using IonDotnet.Internals.Text;
using IonDotnet.Systems;

namespace IonDotnet.Bench
{
    // ReSharper disable once UnusedMember.Global
    public class WriteGround : IRunable
    {
        public void Run(string[] args)
        {
            var writer = new StringWriter();
            var s = new IonTextWriter(writer, new IonTextOptions
            {
                PrettyPrint = true
            });
            s.StepIn(IonType.Struct);
            s.SetFieldName("no");
            s.WriteString("yes");
            s.StepOut();
            s.Finish();
            var r = writer.ToString();
            Console.WriteLine(r);
        }
    }
}
