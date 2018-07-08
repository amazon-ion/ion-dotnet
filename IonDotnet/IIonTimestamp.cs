using System;

namespace IonDotnet
{
    public interface IIonTimestamp : IIonValue<IIonTimestamp>
    {
        DateTime TimeStampValue { get; set; }

        void SetMillis(long epochMillis);

        void SetCurrentTime();

        void MakeNull();
    }
}
