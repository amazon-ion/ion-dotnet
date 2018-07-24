using System;

namespace IonDotnet.Bench
{
    public interface IRunable
    {
        void Run(ArraySegment<string> args);
    }
}
