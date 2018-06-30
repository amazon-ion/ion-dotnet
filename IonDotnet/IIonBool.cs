namespace IonDotnet
{
    /// <summary>
    /// An Ion bool value
    /// </summary>
    public interface IIonBool : IIonValue
    {
        /// <summary>
        /// The boolean value of this <see cref="IIonValue"/>
        /// </summary>
        /// <exception cref="System.NullReferenceException">When getting a 'bool.null' value</exception>
        bool BooleanValue { get; set; }

        /// <summary>
        /// Sets this instance to have a specific value.
        /// </summary>
        /// <param name="val">New value for this 'bool', might be null</param>
        void SetValue(bool? val);

        /// <summary>
        /// Clone this IonBool
        /// </summary>
        IIonBool CloneBool();
    }
}
