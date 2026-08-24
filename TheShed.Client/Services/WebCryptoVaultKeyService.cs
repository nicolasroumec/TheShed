using System.Security.Cryptography;
using Microsoft.JSInterop;
using TheShed.Shared.Security;

namespace TheShed.Client.Services
{
    /// <inheritdoc cref="IVaultKeyService"/>
    public class WebCryptoVaultKeyService : IVaultKeyService
    {
        private const int KeySizeBytes = 32; // AES-256

        private readonly IJSRuntime _js;

        public WebCryptoVaultKeyService(IJSRuntime js) => _js = js;

        public async Task<string> GenerateWrappedKeyAsync(byte[] stretchedMasterKey)
        {
            ArgumentNullException.ThrowIfNull(stretchedMasterKey);

            // RandomNumberGenerator (unlike AesGcm) is managed and runs fine on browser-wasm.
            var vaultKey = RandomNumberGenerator.GetBytes(KeySizeBytes);

            return await _js.InvokeAsync<string>(
                "encryptAesGcm", Convert.ToBase64String(stretchedMasterKey), Convert.ToBase64String(vaultKey));
        }
    }
}
