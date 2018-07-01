namespace IonDotnet
{
    public interface IIonValue<out T> : IIonValue where T : IIonValue
    {
        /// <summary>
        /// Creates a copy of this value and all of its children.
        /// </summary>
        /// <returns>A copy of this value</returns>
        /// <exception cref="UnknownSymbolException" />
        T Clone();
    }

    public interface IIonValue
    {
        /// <summary>
        /// Gets an enumeration value identifying the core Ion data type of this object
        /// </summary>
        IonType Type { get; }

        /// <summary>
        /// Determines whether this object is a Null value
        /// </summary>
        /// <remarks>
        /// There are different Null values such as 'null' or 'null.string' or 'null.bool'
        /// </remarks>
        bool IsNull { get; }

        /// <summary>
        /// Determine whether this value is read-only
        /// </summary>
        /// <remarks>
        /// A read-only IonValue is thread-safe
        /// </remarks>
        bool IsReadOnly { get; }

        /// <summary>
        /// The symbol table used to encode this value.
        /// </summary>
        ISymbolTable SymbolTable { get; }

        /// <summary>
        /// Field name attached to this value
        /// </summary>
        string FieldName { get; }

        /// <summary>
        /// The <see cref="T:IonDotnet.SymbolToken" /> attached to this value as an interned symbol
        /// </summary>
        SymbolToken FieldNameSymbol { get; }

        /// <summary>
        /// The container of this value
        /// </summary>
        IIonContainer Container { get; }

        /// <summary>
        /// Removes this value from its container, if any.
        /// </summary>
        /// <returns>True if this value was in a container before remove</returns>
        bool RemoveFromContainer();

        /// <summary>
        /// The top level value above this value.
        /// </summary>
        IIonValue TopLevelValue { get; }

        /// <summary>
        /// Returns a pretty-printed Ion text representation of this value, using
        /// </summary>
        /// <returns></returns>
        string ToPrettyString();

        /// <summary>
        /// Checks if this container is empty
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Gets this value's user type annotations as text.
        /// </summary>
        /// <returns></returns>
        string[] GetTypeAnnotations();

        /// <summary>
        /// Gets this value's user type annotations as interned <see cref="T:IonDotnet.SymbolToken" />
        /// </summary>
        /// <returns></returns>
        SymbolToken[] GetTypeAnnotationSymbols();

        /// <summary>
        /// Determines whether or not the value is annotated with a particular user type annotation.
        /// </summary>
        /// <param name="annotation">annotation as a string value</param>
        /// <returns>true if this value has the annotation</returns>
        bool HasTypeAnnotation(string annotation);

        /// <summary>
        /// Replaces all type annotations with the given text.
        /// </summary>
        /// <param name="annotations">
        /// Annotations the new annotations.  If null or empty array, then 
        /// all annotations are removed.  Any duplicates are preserved.
        /// </param>
        void SetTypeAnnotations(params string[] annotations);

        void SetTypeAnnotationSymbols(params SymbolToken[] annotations);
        void ClearTypeAnnotations();
        void AddTypeAnnotation(string annotation);
        void RemoveTypeAnnotation(string annotation);
        void WriteTo(IIonWriter writer);
        void Accept(IValueVisitor visitor);
        void MakeReadOnly();
    }
}
