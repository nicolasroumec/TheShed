namespace TheShed.Client.Services
{
    /// <inheritdoc cref="IOwnKeypairCache"/>
    public class OwnKeypairCache : IOwnKeypairCache
    {
        private (string? PublicKey, string? EncryptedPrivateKey) _keypair;

        public void Set(string? publicKey, string? encryptedPrivateKey) =>
            _keypair = (publicKey, encryptedPrivateKey);

        public (string? PublicKey, string? EncryptedPrivateKey) Get() => _keypair;

        public void Clear() => _keypair = (null, null);
    }
}
