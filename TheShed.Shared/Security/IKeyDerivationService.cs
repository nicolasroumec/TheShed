namespace TheShed.Shared.Security
{
    /// <summary>
    /// Derives the client-side "stretched master key" from a user's master password.
    /// This key never leaves the browser and the server never sees it — it is independent
    /// of the Argon2 hash used for login authentication.
    /// </summary>
    public interface IKeyDerivationService
    {
        /// <summary>Generates a random salt for a new user (16 bytes).</summary>
        byte[] GenerateSalt();

        /// <summary>
        /// Derives a 256-bit key from the master password and salt via PBKDF2-SHA256.
        /// Deterministic: the same password + salt always produce the same key. Async because
        /// the client-side implementation goes through JS interop (Web Crypto) rather than
        /// running 600k iterations synchronously on the WASM main thread.
        /// </summary>
        Task<byte[]> DeriveKeyAsync(string password, byte[] salt);
    }
}
