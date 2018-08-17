namespace IonDotnet.Internals
{
    internal interface IPrivateWriter : IIonWriter
    {
        bool IsFieldNameSet();

        int GetDepth();

        void WriteIonVersionMarker();

        bool IsStreamCopyOptimized { get; }
    }
}
