using System.Security.Cryptography;

namespace TheShed.Shared.Security
{
    /// <summary>
    /// Synchronous AES-256-GCM. Generates a random nonce per operation and returns
    /// <c>base64(nonce(12) || ciphertext || tag(16))</c>. The key (32 bytes) arrives already
    /// resolved — this type does not know where it comes from.
    /// <para>
    /// No longer runs in production (D3, superseded by D7 — the zero-knowledge model has no
    /// server key). It stays as a test utility: <see cref="AesGcm"/> throws
    /// <see cref="PlatformNotSupportedException"/> on browser-wasm, so the real client
    /// encrypts/decrypts through Web Crypto (JS interop, see <c>WebCryptoUserKeypairService</c> in
    /// <c>TheShed.Client.Services</c>) — this class simulates that side in tests running on the
    /// regular runtime (<c>ZeroKnowledgeE2ETests</c>), with the same byte layout
    /// (nonce||ciphertext||tag).
    /// </para>
    /// </summary>
    public class AesEncryptionService
    {
        private const int KeySize = 32;   // AES-256
        private const int NonceSize = 12; // 96 bits, recommended for GCM
        private const int TagSize = 16;   // 128 bits

        private readonly byte[] _key;

        public AesEncryptionService(byte[] key)
        {
            ArgumentNullException.ThrowIfNull(key);
            if (key.Length != KeySize)
            {
                throw new ArgumentException(
                    $"The key must be {KeySize} bytes (AES-256); got {key.Length}.", nameof(key));
            }

            _key = key;
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
                throw new ArgumentException("Invalid ciphertext: too short.", nameof(ciphertext));
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
