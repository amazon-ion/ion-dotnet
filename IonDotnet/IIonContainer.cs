using System.Collections.Generic;

namespace IonDotnet
{
    /// <inheritdoc cref="IIonValue" />
    /// <summary>
    /// Common functionality of Ion struct, list, and sexp
    /// </summary>
    public interface IIonContainer : IIonValue, ICollection<IIonValue>
    {
        /// <summary>
        /// Sets the contents of this container to an Ion null
        /// </summary>
        void MakeNull();

//        /// <summary>
//        /// Checks if this container is empty
//        /// </summary>
//        bool IsEmpty { get; }

//        /// <summary>
//        /// Removes the given element from this container.
//        /// </summary>
//        /// <param name="element">Child element</param>
//        /// <returns>True if the container contains the element</returns>
//        bool RemoveChild(IIonValue element);
    }
}
