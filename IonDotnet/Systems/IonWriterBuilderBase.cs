using System;
using System.IO;

namespace IonDotnet.Systems
{
    /// <summary>
    /// Common options for writing Ion data streams of any form.
    /// </summary>
    public abstract class IonWriterBuilderBase
    {
        /// <summary>
        /// A strategy for altering emission of Ion version markers at the start of an Ion stream.
        /// </summary>
        public enum InitialIvmHandlingOption
        {
            /// <summary>
            /// IVMs are emitted only when explicitly written or when necessary
            /// </summary>
            Default,

            /// <summary>
            /// Always emits an initial IVM, even when the user hasn't explicitly written one. If the user
            /// <em>does</em> write one, this won't cause an extra to be emitted.
            /// </summary>
            Ensure,

            /// <summary>
            /// Indicates that initial IVMs should be suppressed from the output stream whenever possible,
            /// even when they are explicitly written.
            /// </summary>
            Suppress
        }

        /// <summary>
        /// A strategy for minimizing the output of non-initial Ion version markers.
        /// </summary>
        public enum IvmMinimizingOption
        {
            Default,

            /// <summary>
            /// Replaces identical, adjacent IVMs with a single IVM. 
            /// </summary>
            Adjacent,

            /// <summary>
            /// Discards IVMs that don't change the Ion version, even when there's other data between 
            /// them. This includes adjacent IVMs.
            /// </summary>
            Distant
        }

        private ICatalog _myCatalog;
        private ISymbolTable[] _myImports;

        /** NOT FOR APPLICATION USE! */
        protected IonWriterBuilderBase(IonWriterBuilderBase that)
        {
            _myCatalog = that._myCatalog;
            _myImports = that._myImports;
        }

        public ICatalog Catalog
        {
            get => _myCatalog;
            set
            {
                MutationCheck();
                _myCatalog = value;
            }
        }

        public ISymbolTable[] Imports
        {
            get => _myImports;
            set
            {
                MutationCheck();
                _myImports = SafeCopy(value);
            }
        }

        /// <summary>
        /// strategy for emitting Ion version markers at the start of the stream.
        /// </summary>
        /// <remarks>
        /// By default, IVMs are emitted only when explicitly written or when necessary
        /// (for example, before data that's not Ion 1.0, or at the start of Ion binary output).
        /// </remarks>
        public abstract InitialIvmHandlingOption InitialIvmHandling { get; }

        /// <summary>
        /// strategy for eliminating Ion version markers mid-stream.
        /// </summary>
        /// <remarks>
        /// By default, IVMs are emitted as received or when necessary.
        /// This strategy does not affect handling of IVMs at the start of the stream;
        /// </remarks>
        public abstract IvmMinimizingOption IvmMinimizing { get; }

        /// <summary>
        /// Builds a new writer based on this builder's configuration properties.
        /// </summary>
        /// <param name="output">the stream that will receive Ion data. Must not be null.</param>
        /// <returns>a new <see cref="IIonWriter"/> instance; not {@code null}.</returns>
        public abstract IIonWriter Build(Stream output);

        protected virtual void MutationCheck() => throw new InvalidOperationException("This builder is immutable");

        private static ISymbolTable[] SafeCopy(ISymbolTable[] imports)
        {
            if (imports != null && imports.Length != 0)
            {
                imports = (ISymbolTable[]) imports.Clone();
            }

            return imports;
        }
    }
}
