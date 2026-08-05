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
            return Convert.ToBase64String(EncryptBytes(System.Text.Encoding.UTF8.GetBytes(plaintext)));
        }

        public string Decrypt(string ciphertext)
        {
            ArgumentException.ThrowIfNullOrEmpty(ciphertext);
            return System.Text.Encoding.UTF8.GetString(DecryptBytes(Convert.FromBase64String(ciphertext)));
        }

        public byte[] EncryptBytes(byte[] plaintext)
        {
            ArgumentNullException.ThrowIfNull(plaintext);

            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var cipherBytes = new byte[plaintext.Length];
            var tag = new byte[TagSize];

            using var aes = new AesGcm(_key, TagSize);
            aes.Encrypt(nonce, plaintext, cipherBytes, tag);

            // Layout: nonce || ciphertext || tag
            var output = new byte[NonceSize + cipherBytes.Length + TagSize];
            Buffer.BlockCopy(nonce, 0, output, 0, NonceSize);
            Buffer.BlockCopy(cipherBytes, 0, output, NonceSize, cipherBytes.Length);
            Buffer.BlockCopy(tag, 0, output, NonceSize + cipherBytes.Length, TagSize);

            return output;
        }

        public byte[] DecryptBytes(byte[] ciphertext)
        {
            ArgumentNullException.ThrowIfNull(ciphertext);
            if (ciphertext.Length < NonceSize + TagSize)
            {
                throw new ArgumentException("Texto cifrado inválido: longitud insuficiente.", nameof(ciphertext));
            }

            var cipherLength = ciphertext.Length - NonceSize - TagSize;
            var nonce = new byte[NonceSize];
            var cipherBytes = new byte[cipherLength];
            var tag = new byte[TagSize];

            Buffer.BlockCopy(ciphertext, 0, nonce, 0, NonceSize);
            Buffer.BlockCopy(ciphertext, NonceSize, cipherBytes, 0, cipherLength);
            Buffer.BlockCopy(ciphertext, NonceSize + cipherLength, tag, 0, TagSize);

            var plainBytes = new byte[cipherLength];
            using var aes = new AesGcm(_key, TagSize);
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

            return plainBytes;
        }
    }
}
