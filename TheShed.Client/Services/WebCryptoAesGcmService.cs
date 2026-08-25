using Microsoft.JSInterop;
using TheShed.Shared.Security;

namespace TheShed.Client.Services
{
    /// <inheritdoc cref="IAesGcmService"/>
    public class WebCryptoAesGcmService : IAesGcmService
    {
        private readonly IJSRuntime _js;

        public WebCryptoAesGcmService(IJSRuntime js) => _js = js;

        public Task<string> EncryptAsync(byte[] key, string plaintext)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(plaintext);

            return _js.InvokeAsync<string>(
                "encryptAesGcm", Convert.ToBase64String(key), Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plaintext))).AsTask();
        }

        public async Task<string> DecryptAsync(byte[] key, string ciphertext)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentException.ThrowIfNullOrEmpty(ciphertext);

            var plaintextBase64 = await _js.InvokeAsync<string>(
                "decryptAesGcm", Convert.ToBase64String(key), ciphertext);
            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(plaintextBase64));
        }
    }
}
