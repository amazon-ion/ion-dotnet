using System;
using System.IO;
using System.Text;
using IonDotnet.Systems;

namespace IonDotnet.Tests.Integration
{
    public enum InputStyle
    {
        MemoryStream,
        FileStream,
        Text
    }

    public static class TestReader
    {
        public static IIonReader FromFile(FileInfo file, InputStyle style)
        {
            switch (style)
            {
                case InputStyle.MemoryStream:
                    var bytes = File.ReadAllBytes(file.FullName);
                    return IonReaderBuilder.Build(new MemoryStream(bytes));
                case InputStyle.FileStream:
                    throw new NotImplementedException();
                case InputStyle.Text:
                    var str = File.ReadAllText(file.FullName, Encoding.UTF8);
                    return IonReaderBuilder.Build(str);
                default:
                    throw new ArgumentOutOfRangeException(nameof(style), style, null);
            }
        }
    }
}
