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
                PrettyPrint = false
            });
            s.StepIn(IonType.Struct);
            s.SetFieldName("no");
            s.StepIn(IonType.Struct);
            s.SetFieldName("lo");
            s.WriteString("yes");
            s.SetFieldName("num");
            s.WriteInt(23321);
            s.SetFieldName("bool");
            s.WriteBool(true);
            s.SetFieldName("float");
            s.WriteFloat(4.2312321);
            s.SetFieldName("symbol");
            s.WriteSymbol("dadasdSym");
            s.SetFieldName("datetime");
            s.WriteTimestamp(new Timestamp(DateTime.Now));
            s.StepOut();
            
            s.SetFieldName("listint");
            s.StepIn(IonType.List);
            
            s.StepIn(IonType.Struct);
            s.SetFieldName("int");
            s.WriteInt(1);
            s.StepOut();
            
            s.StepIn(IonType.Struct);
            s.SetFieldName("int");
            s.WriteInt(2);
            s.StepOut();
            
            s.StepIn(IonType.Struct);
            s.SetFieldName("int");
            s.WriteInt(3);
            s.StepOut();
            
            s.StepOut();
            
            s.StepOut();
            s.Finish();
            var r = writer.ToString();
            Console.WriteLine(r);
        }
    }
}
