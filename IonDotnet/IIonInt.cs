using System;
using System.Numerics;

namespace IonDotnet
{
    public interface IIonInt : IIonValue<IIonInt>
    {
        /// <summary>
        /// Integer value
        /// </summary>
        /// <remarks>This might throw <see cref="OverflowException"/> if the integersize is larger than 32bit></remarks>
        int IntValue { get; set; }

        /// <summary>
        /// Long value
        /// </summary>
        /// <remarks>This might throw <see cref="OverflowException"/> if the integersize is larger than 64bit></remarks>
        long LongValue { get; set; }

        /// <summary>
        /// BigInterger value
        /// </summary>
        BigInteger BigIntegerValue { get; set; }

        void SetNull();

        IntegerSize Size { get; }
    }
}
