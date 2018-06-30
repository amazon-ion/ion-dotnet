using System.Collections.Generic;

namespace IonDotnet
{
    /// <inheritdoc cref="IIonValue" />
    /// <summary>
    /// Common functionality of Ion struct, list, and sexp
    /// </summary>
    public interface IIonContainer : IIonValue, IEnumerable<IIonValue>
    {
        /// <summary>
        /// Sets the contents of this container to an Ion null
        /// </summary>
        void MakeNull();
    }
}
