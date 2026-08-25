namespace TheShed.Shared.Security
{
    /// <summary>
    /// Encrypts/decrypts arbitrary text client-side with an already-available AES-256 key (a
    /// vault key, not a stretched master key — see <see cref="IVaultKeyService"/> for wrapping
    /// the key itself). Backs Sprint 26's move of entry/note field encryption to the client: the
    /// server only ever stores and returns the resulting blob. Asynchronous for the same reason
    /// as <see cref="IVaultKeyService"/> — the browser implementation delegates to the Web
    /// Crypto API (<see cref="System.Security.Cryptography.AesGcm"/> throws
    /// <see cref="PlatformNotSupportedException"/> on browser-wasm).
    /// </summary>
    public interface IAesGcmService
    {
        /// <summary>Encrypts <paramref name="plaintext"/> with <paramref name="key"/> (AES-256-GCM,
        /// same wire format as <see cref="IEncryptionService"/>): base64(nonce(12)||ciphertext||tag(16)).</summary>
        Task<string> EncryptAsync(byte[] key, string plaintext);

        /// <summary>Reverses <see cref="EncryptAsync"/>.</summary>
        Task<string> DecryptAsync(byte[] key, string ciphertext);
    }
}
