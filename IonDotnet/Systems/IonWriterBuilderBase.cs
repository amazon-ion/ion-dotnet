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
    }
}
