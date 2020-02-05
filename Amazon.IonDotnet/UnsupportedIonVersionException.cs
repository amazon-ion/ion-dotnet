// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Amazon.IonDotnet
{
    public class UnsupportedIonVersionException : IonException
    {
        public UnsupportedIonVersionException(string version) : base($"Unsupported Ion version {version}")
        {
            Version = version;
        }

        public string Version { get; }
    }
}
