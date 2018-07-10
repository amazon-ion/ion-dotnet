namespace IonDotnet.Systems
{
    public abstract class IonBinaryWriterBuilder : IonWriterBuilderBase<IonBinaryWriterBuilder>
    {
        protected IonBinaryWriterBuilder(IonBinaryWriterBuilder that) : base(that)
        {
        }

        public override InitialIvmHandlingOption InitialIvmHandling => InitialIvmHandlingOption.Ensure;

        public override IvmMinimizingOption IvmMinimizing => IvmMinimizingOption.Default;

        public abstract ISymbolTable InitialSymbolTable { get; set; }

        public abstract IonBinaryWriterBuilder WithInitialSymbolTable(ISymbolTable symtab);

        /// <summary>
        /// Enables or disables writing Binary32 (4-byte, single precision, IEEE-754) values for floats
        /// when there would be no loss in precision. By default Binary32 support is disabled to ensure
        /// the broadest compatibility with existing Ion implementations. Historically, implementations
        /// were only able to read Binary64 values.
        /// </summary>
        /// <remarks>
        /// When enabled, floats are evaluated for a possible loss of data at single precision. If the
        /// value can be represented in single precision without data loss, it is written as a 4-byte,
        /// Binary32 value. Floats which cannot be represented as single-precision values are written as
        /// 8-byte, Binary64 values (this is the legacy behavior for all floats, regardless of value).
        /// </remarks>
        public abstract bool IsFloatBinary32Enabled { set; }

        public abstract IonBinaryWriterBuilder WithFloatBinary32Enabled();

        public abstract IonBinaryWriterBuilder WithFloatBinary32Disabled();
    }
}
