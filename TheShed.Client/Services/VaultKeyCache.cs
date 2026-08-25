namespace TheShed.Client.Services
{
    /// <inheritdoc cref="IVaultKeyCache"/>
    public class VaultKeyCache : IVaultKeyCache
    {
        private readonly Dictionary<int, byte[]> _keys = [];

        public void Set(int vaultId, byte[] key) => _keys[vaultId] = key;
        public byte[]? Get(int vaultId) => _keys.GetValueOrDefault(vaultId);
        public void Clear() => _keys.Clear();
    }
}
