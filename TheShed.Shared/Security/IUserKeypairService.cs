namespace TheShed.Shared.Security
{
    /// <summary>Public key (PEM) and AES-GCM-wrapped private key (base64) for a user's keypair.</summary>
    public record UserKeypair(string PublicKeyPem, string EncryptedPrivateKey);

    /// <summary>
    /// Generates the per-user RSA keypair used for zero-knowledge vault sharing (Sprint 27):
    /// the public key wraps a vault key for a member, the private key unwraps it. Runs
    /// client-side — the server only ever sees the public key and the encrypted private key.
    /// Asynchronous because the browser implementation delegates keygen to the Web Crypto API
    /// (<c>RSA.Create()</c> throws <see cref="PlatformNotSupportedException"/> on browser-wasm —
    /// there is no native crypto provider backing it there).
    /// </summary>
    public interface IUserKeypairService
    {
        /// <summary>
        /// Generates a new RSA keypair and encrypts the private key with
        /// <paramref name="stretchedMasterKey"/> (AES-256-GCM, same wire format as
        /// <see cref="IEncryptionService"/>).
        /// </summary>
        Task<UserKeypair> GenerateAsync(byte[] stretchedMasterKey);

        /// <summary>Wraps <paramref name="vaultKey"/> (RSA-OAEP) with a member's public key, so
        /// the owner can share a vault with them (Sprint 27) — the server only ever stores the
        /// result as an opaque <c>VaultKeyWrap.WrappedKey</c> blob.</summary>
        Task<string> WrapKeyForMemberAsync(byte[] vaultKey, string memberPublicKeyPem);

        /// <summary>Reverses <see cref="WrapKeyForMemberAsync"/> from the member's side: decrypts
        /// their own <paramref name="encryptedPrivateKey"/> with <paramref name="stretchedMasterKey"/>
        /// (same wrap <see cref="GenerateAsync"/> produced), then RSA-OAEP-decrypts
        /// <paramref name="wrappedVaultKey"/> with it.</summary>
        Task<byte[]> UnwrapKeyAsMemberAsync(byte[] stretchedMasterKey, string encryptedPrivateKey, string wrappedVaultKey);
    }
}
