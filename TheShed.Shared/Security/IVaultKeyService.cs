namespace TheShed.Shared.Security
{
    /// <summary>
    /// Generates a vault's AES-256 encryption key ("vault key") client-side and wraps it with
    /// the owner's stretched master key, so the server only ever sees the wrapped blob
    /// (<c>VaultKeyWrap.WrappedKey</c>) — see Sprint 26. Asynchronous for the same reason as
    /// <see cref="IUserKeypairService"/>: the browser implementation delegates the AES-GCM wrap
    /// to the Web Crypto API (<see cref="System.Security.Cryptography.AesGcm"/> throws
    /// <see cref="PlatformNotSupportedException"/> on browser-wasm).
    /// </summary>
    public interface IVaultKeyService
    {
        /// <summary>
        /// Generates a random 256-bit vault key and returns it wrapped (AES-256-GCM, same wire
        /// format as <see cref="IEncryptionService"/>) with <paramref name="stretchedMasterKey"/>.
        /// </summary>
        Task<string> GenerateWrappedKeyAsync(byte[] stretchedMasterKey);
    }
}
