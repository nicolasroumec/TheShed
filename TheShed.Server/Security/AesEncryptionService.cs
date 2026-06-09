using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace TheShed.Server.Security
{
    /// <summary>
    /// Implementación de <see cref="IEncryptionService"/> con AES-256-GCM.
    /// Genera un nonce aleatorio por operación y devuelve
    /// <c>base64(nonce(12) || ciphertext || tag(16))</c>.
    /// La clave (32 bytes) se obtiene de <see cref="EncryptionSettings"/>.
    /// </summary>
    public class AesEncryptionService : IEncryptionService
    {
        private const int KeySize = 32;   // AES-256
        private const int NonceSize = 12; // 96 bits, recomendado para GCM
        private const int TagSize = 16;   // 128 bits

        private readonly byte[] _key;

        public AesEncryptionService(IOptions<EncryptionSettings> settings)
        {
            var raw = settings.Value.Key;
            if (string.IsNullOrWhiteSpace(raw))
            {
                throw new InvalidOperationException(
                    "Falta la clave de cifrado (Encryption:Key). Configurarla por User Secrets o variables de entorno.");
            }

            _key = Convert.FromBase64String(raw);
            if (_key.Length != KeySize)
            {
                throw new InvalidOperationException(
                    $"La clave de cifrado debe decodificar a {KeySize} bytes (AES-256); se obtuvieron {_key.Length}.");
            }
        }

        public string Encrypt(string plaintext)
        {
            ArgumentNullException.ThrowIfNull(plaintext);

            var plainBytes = System.Text.Encoding.UTF8.GetBytes(plaintext);
            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var cipherBytes = new byte[plainBytes.Length];
            var tag = new byte[TagSize];

            using var aes = new AesGcm(_key, TagSize);
            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

            // Layout: nonce || ciphertext || tag
            var output = new byte[NonceSize + cipherBytes.Length + TagSize];
            Buffer.BlockCopy(nonce, 0, output, 0, NonceSize);
            Buffer.BlockCopy(cipherBytes, 0, output, NonceSize, cipherBytes.Length);
            Buffer.BlockCopy(tag, 0, output, NonceSize + cipherBytes.Length, TagSize);

            return Convert.ToBase64String(output);
        }

        public string Decrypt(string ciphertext)
        {
            ArgumentException.ThrowIfNullOrEmpty(ciphertext);

            var data = Convert.FromBase64String(ciphertext);
            if (data.Length < NonceSize + TagSize)
            {
                throw new ArgumentException("Texto cifrado inválido: longitud insuficiente.", nameof(ciphertext));
            }

            var cipherLength = data.Length - NonceSize - TagSize;
            var nonce = new byte[NonceSize];
            var cipherBytes = new byte[cipherLength];
            var tag = new byte[TagSize];

            Buffer.BlockCopy(data, 0, nonce, 0, NonceSize);
            Buffer.BlockCopy(data, NonceSize, cipherBytes, 0, cipherLength);
            Buffer.BlockCopy(data, NonceSize + cipherLength, tag, 0, TagSize);

            var plainBytes = new byte[cipherLength];
            using var aes = new AesGcm(_key, TagSize);
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

            return System.Text.Encoding.UTF8.GetString(plainBytes);
        }
    }
}
