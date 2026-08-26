namespace TheShed.Client.Services
{
    /// <summary>
    /// Caches the current user's own RSA public key and AES-wrapped private key in memory for
    /// the lifetime of the authenticated session (Sprint 27) — set right after login/register
    /// (both already have it in hand, no extra round trip) and read when a shared member needs
    /// to unwrap a vault key wrapped for them. Never persisted; cleared on logout, same as
    /// <see cref="IStretchedKeyStore"/> and <see cref="IVaultKeyCache"/>.
    /// </summary>
    public interface IOwnKeypairCache
    {
        void Set(string? publicKey, string? encryptedPrivateKey);
        (string? PublicKey, string? EncryptedPrivateKey) Get();
        void Clear();
    }
}
