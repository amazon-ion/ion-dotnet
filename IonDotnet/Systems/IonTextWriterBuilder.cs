namespace IonDotnet.Systems
{
    internal abstract class IonTextWriterBuilder : IonWriterBuilderBase<IonTextWriterBuilder>
    {
        protected IonTextWriterBuilder(IonTextWriterBuilder that) : base(that)
        {
        }
    }
}
