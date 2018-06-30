using System.IO;
using System.Text;

namespace IonDotnet
{
    public interface IIonClob : IIonLob<IIonClob>
    {
        TextReader GeTextReader(Encoding encoding);
        string GetStringValue(Encoding encoding);
    }
}
