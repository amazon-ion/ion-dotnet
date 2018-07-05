namespace IonDotnet
{
    /// <summary>
    /// Loads Ion data in the form of datagrams.  These methods parse the input in its entirety to identify problems immediately. 
    /// </summary>
    /// <remarks>
    /// In contrast, an <see cref="IIonReader"/>  will parse one top-level value at a time, and is better suited
    /// for streaming protocols or large inputs.
    /// </remarks>
    public interface ILoader
    {
        /// <summary>
        /// The <see cref="IIonSystem"/> from which this loader was created.
        /// </summary>
        IIonSystem IonSystem { get; }

        /// <summary>
        /// Catalog being used by this loader.
        /// </summary>
        ICatalog Catalog { get; }

        /// <summary>
        /// Loads Ion text in its entirety.
        /// </summary>
        /// <param name="ionText">Text</param>
        /// <returns>Ion datagram</returns>
        /// <exception cref="IonException">when there is syntax error</exception>
        IIonDatagram Load(string ionText);

        /// <summary>
        /// Loads a block of Ion data into a single datagram, detecting whether it's text or binary data.
        /// </summary>
        /// <param name="ionData"> Ion binary data, or UTF-8 Ion text.</param>
        /// <returns>a datagram containing all the values on the input stream;</returns>
        /// <remarks>This method assumes ownership of the array and may modify it </remarks>
        IIonDatagram Load(byte[] ionData);
    }
}
