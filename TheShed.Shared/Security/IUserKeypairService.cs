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
    }
}
