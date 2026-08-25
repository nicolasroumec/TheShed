using System.Security.Cryptography;

namespace TheShed.Shared.Security
{
    /// <inheritdoc cref="IKeyDerivationService"/>
    public class KeyDerivationService : IKeyDerivationService
    {
        private const int SaltSize = 16;
        private const int KeySize = 32; // 256 bits

        // ponytail: PBKDF2 instead of Argon2 client-side — Isopoh.Cryptography.Argon2 is
        // native (P/Invoke) and doesn't run in WASM without extra research. 600k iterations
        // meets the OWASP 2023 floor for PBKDF2-SHA256; upgrade to Argon2id if a WASM-capable
        // lib shows up.
        private const int Iterations = 600_000;

        public byte[] GenerateSalt() => RandomNumberGenerator.GetBytes(SaltSize);

        // Runs on the real .NET runtime (server, tests) — Rfc2898DeriveBytes is native there and
        // this is fast. The client uses WebCryptoKeyDerivationService instead: this managed
        // implementation is 600k iterations of interpreted WASM if used from the browser, which
        // measured ~70-90s of a fully frozen tab (single-threaded WASM blocks rendering and input
        // for the duration).
        public Task<byte[]> DeriveKeyAsync(string password, byte[] salt)
        {
            ArgumentException.ThrowIfNullOrEmpty(password);
            ArgumentNullException.ThrowIfNull(salt);
            return Task.FromResult(Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize));
        }
    }
}
