using System.Collections.Generic;

namespace IonDotnet
{
    /// <inheritdoc />
    /// <summary>
    /// An IonStruct is a IonValue that contains fields, which can have the duplicate names
    /// </summary>
    /// <remarks>
    /// Operation <see cref="Add(string)"/> can result in repeated field names
    /// Operation <see cref="Put(string)"/> replaces all fields with the same name
    /// </remarks>
    public interface IIonStruct : IIonContainer
    {
        int Size { get; }

        bool ContainsKey(string fieldName);

        bool ContainsValue(IIonValue value);

        IIonStruct Get(string fieldName);

        /// <summary>
        /// Replace all fields with name <see cref="fieldName"/> with the value
        /// </summary>
        /// <param name="fieldName">Name of the new field</param>
        /// <param name="child">Value of the new field</param>
        /// <exception cref="ContainedValueException">When <see cref="child"/> already has a container</exception>
        void Put(string fieldName, IIonStruct child);

        IValueFactory Put(string fieldName);

        void PutAll(IDictionary<string, IIonStruct> members);

        /// <summary>
        /// Add a new field with name <see cref="fieldName"/> with the value
        /// </summary>
        /// <param name="fieldName">Name of the new field</param>
        /// <param name="child">Value of the new field</param>
        /// <exception cref="ContainedValueException">When <see cref="child"/> already has a container</exception>
        /// <remarks>Results in repeated field if <see cref="fieldName"/> already exists</remarks>
        void Add(string fieldName, IIonStruct child);

        IValueFactory Add(string fieldName);

        IIonStruct Remove(string fieldName);

        bool RemoveAll(params string[] fieldNames);

        bool Retains(params string[] fieldNames);

        IIonStruct Clone();

        IIonStruct CloneAndRemove(params string[] fieldNames);

        IIonStruct CloneAndRetains(params string[] fieldNames);
    }
}
