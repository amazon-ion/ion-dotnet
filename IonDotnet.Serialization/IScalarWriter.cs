namespace IonDotnet.Serialization
{
    public interface IScalarWriter
    {
        /// <summary>
        /// Try to write the value
        /// </summary>
        /// <param name="valueWriter"></param>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="value"></param>
        /// <returns>True if the written is done</returns>
        bool TryWriteValue<T>(IValueWriter valueWriter, T value);
    }
}
