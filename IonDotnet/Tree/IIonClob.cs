using System.IO;
using System.Text;

namespace IonDotnet.Tree
{
    public interface IIonClob : IIonLob
    {
        StreamReader NewReader(Encoding encoding);
    }
}
