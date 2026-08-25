using System.Security.Cryptography;
using Microsoft.JSInterop;
using TheShed.Shared.Security;

namespace TheShed.Client.Services
{
    /// <inheritdoc cref="IKeyDerivationService"/>
    public class WebCryptoKeyDerivationService : IKeyDerivationService
    {
        private const int SaltSize = 16;
        private const int KeySizeBits = 256;

        // Same iteration count as KeyDerivationService (OWASP 2023 floor for PBKDF2-SHA256) —
        // this only changes who runs the derivation, not the security level.
        private const int Iterations = 600_000;

        private readonly IJSRuntime _js;

        public WebCryptoKeyDerivationService(IJSRuntime js) => _js = js;

        public byte[] GenerateSalt() => RandomNumberGenerator.GetBytes(SaltSize);

        public async Task<byte[]> DeriveKeyAsync(string password, byte[] salt)
        {
            ArgumentException.ThrowIfNullOrEmpty(password);
            ArgumentNullException.ThrowIfNull(salt);

            var derivedBase64 = await _js.InvokeAsync<string>(
                "deriveKeyPbkdf2", password, Convert.ToBase64String(salt), Iterations, KeySizeBits);
            return Convert.FromBase64String(derivedBase64);
        }
    }
}
