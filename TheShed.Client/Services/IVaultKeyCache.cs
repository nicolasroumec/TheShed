namespace TheShed.Client.Services
{
    /// <summary>
    /// Caches unwrapped vault AES keys in memory, keyed by vault id, for the lifetime of the
    /// authenticated session (Sprint 26) — so opening a vault a second time doesn't need another
    /// unwrap round trip. Never persisted; cleared on logout, same as <see cref="IStretchedKeyStore"/>.
    /// </summary>
    public interface IVaultKeyCache
    {
        void Set(int vaultId, byte[] key);
        byte[]? Get(int vaultId);
        void Clear();
    }
}
