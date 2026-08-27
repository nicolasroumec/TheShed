using System.Security.Cryptography;
using Microsoft.JSInterop;
using TheShed.Shared.Security;

namespace TheShed.Client.Services
{
    /// <inheritdoc cref="IUserKeypairService"/>
    public class WebCryptoUserKeypairService : IUserKeypairService
    {
        private const int KeySizeBits = 3072;

        private readonly IJSRuntime _js;

        public WebCryptoUserKeypairService(IJSRuntime js)
        {
            _js = js;
        }

        public async Task<UserKeypair> GenerateAsync(byte[] stretchedMasterKey)
        {
            ArgumentNullException.ThrowIfNull(stretchedMasterKey);

            var raw = await _js.InvokeAsync<RawRsaKeyPair>("generateRsaKeypair", KeySizeBits);
            var publicKeyPem = FormatPublicKeyPem(raw.PublicKeySpkiBase64);

            var encryptedPrivateKey = await _js.InvokeAsync<string>(
                "encryptAesGcm", Convert.ToBase64String(stretchedMasterKey), raw.PrivateKeyPkcs8Base64);

            return new UserKeypair(publicKeyPem, encryptedPrivateKey);
        }

        /// <summary>
        /// SPKI (DER, base64) to PEM. Split out from <see cref="GenerateAsync"/> so it's
        /// unit-testable without a browser.
        /// </summary>
        public static string FormatPublicKeyPem(string publicKeySpkiBase64) =>
            PemEncoding.WriteString("PUBLIC KEY", Convert.FromBase64String(publicKeySpkiBase64));

        public async Task<string> WrapKeyForMemberAsync(byte[] vaultKey, string memberPublicKeyPem)
        {
            ArgumentNullException.ThrowIfNull(vaultKey);
            return await _js.InvokeAsync<string>(
                "wrapKeyRsaOaep", memberPublicKeyPem, Convert.ToBase64String(vaultKey));
        }

        public async Task<byte[]> UnwrapKeyAsMemberAsync(byte[] stretchedMasterKey, string encryptedPrivateKey, string wrappedVaultKey)
        {
            ArgumentNullException.ThrowIfNull(stretchedMasterKey);

            var privateKeyPkcs8Base64 = await _js.InvokeAsync<string>(
                "decryptAesGcm", Convert.ToBase64String(stretchedMasterKey), encryptedPrivateKey);
            var vaultKeyBase64 = await _js.InvokeAsync<string>(
                "unwrapKeyRsaOaep", privateKeyPkcs8Base64, wrappedVaultKey);
            return Convert.FromBase64String(vaultKeyBase64);
        }

        private record RawRsaKeyPair(string PublicKeySpkiBase64, string PrivateKeyPkcs8Base64);
    }
}
