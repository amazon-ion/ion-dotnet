namespace IonDotnet.Internals
{
    internal interface IPrivateIonContainer : IIonContainer
    {
        int GetChildCount();
        IIonValue GetChild(int index);
    }
}
