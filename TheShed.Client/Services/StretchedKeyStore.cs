namespace TheShed.Client.Services
{
    /// <inheritdoc cref="IStretchedKeyStore"/>
    public class StretchedKeyStore : IStretchedKeyStore
    {
        private byte[]? _key;

        public void Set(byte[] key) => _key = key;
        public byte[]? Get() => _key;
        public void Clear() => _key = null;
    }
}
