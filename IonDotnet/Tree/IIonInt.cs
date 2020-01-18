using System.Numerics;

namespace IonDotnet.Tree
{
    public interface IIonInt
    {
        IntegerSize IntegerSize { get; }
        BigInteger BigIntegerValue { get; }
        int IntValue { get; }
        long LongValue { get; }
    }
}
