namespace IonDotnet.Internals
{
    internal interface IPrivateWriter : IIonWriter
    {
        ICatalog Catalog { get; }

        bool IsFieldNameSet();

        int GetDepth();

        void WriteIonVersionMarker();

        bool IsStreamCopyOptimized();
    }
}
