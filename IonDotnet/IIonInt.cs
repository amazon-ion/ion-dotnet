using System.Numerics;

namespace IonDotnet
{
    public interface IIonInt : IIonValue<IIonInt>
    {
        int IntValue { get; set; }

        long LongValue { get; set; }

        BigInteger BigIntegerValue { get; set; }

        void SetNull();

        IntegerSize Size { get; }
    }
}
