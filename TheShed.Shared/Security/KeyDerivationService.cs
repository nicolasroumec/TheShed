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

        public byte[] DeriveKey(string password, byte[] salt)
        {
            ArgumentException.ThrowIfNullOrEmpty(password);
            ArgumentNullException.ThrowIfNull(salt);
            return Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        }
    }
}
