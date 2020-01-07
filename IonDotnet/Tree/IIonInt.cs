using System.Numerics;

namespace IonDotnet.Tree
{
    public interface IIonInt
    {
        IntegerSize IntegerSize { get; }
        BigInteger BigIntegerValue { get; set; }
        int IntValue { get; set; }
        long LongValue { get; set; }
    }
}
