using System;

namespace IonDotnet
{
    /// <inheritdoc />
    /// <summary>
    /// Wrap C# enumerable with iterator
    /// </summary>
    /// <typeparam name="T">Has to be reference type</typeparam>
    /// <remarks>This is a temporary solution</remarks>
    public interface IIterator<out T> : IDisposable where T : class
    {
        bool HasNext();
        T Next();
    }
}
