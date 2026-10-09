using System.Security.Cryptography;
using TheShed.Shared.Security;
using Xunit;

namespace TheShed.Tests.Security
{
    public class AesEncryptionServiceTests
    {
        private static AesEncryptionService CreateService() =>
            new(RandomNumberGenerator.GetBytes(32));

        [Theory]
        [InlineData("super-secret-pässword")]
        [InlineData("")]
        [InlineData("áéíóú 🔐 with symbols & spaces")]
        public void Encrypt_Then_Decrypt_ReturnsOriginal(string plaintext)
        {
            var service = CreateService();

            var cipher = service.Encrypt(plaintext);
            var result = service.Decrypt(cipher);

            Assert.Equal(plaintext, result);
        }

        [Fact]
        public void Encrypt_SameText_ProducesDifferentCiphertexts()
        {
            var service = CreateService();

            var a = service.Encrypt("same-text");
            var b = service.Encrypt("same-text");

            // Random nonce per operation → the ciphertexts must differ.
            Assert.NotEqual(a, b);
        }

        [Fact]
        public void Decrypt_TamperedData_Throws()
        {
            var service = CreateService();
            var cipher = service.Encrypt("intact-data");

            // Tamper with one payload byte (flip the tag's last byte).
            var bytes = Convert.FromBase64String(cipher);
            bytes[^1] ^= 0xFF;
            var tampered = Convert.ToBase64String(bytes);

            Assert.Throws<AuthenticationTagMismatchException>(() => service.Decrypt(tampered));
        }

        [Fact]
        public void Decrypt_WithOtherKey_Throws()
        {
            var cipher = CreateService().Encrypt("secret");
            var otherService = CreateService(); // different key

            Assert.Throws<AuthenticationTagMismatchException>(() => otherService.Decrypt(cipher));
        }

        [Fact]
        public void Constructor_WrongKeyLength_Throws()
        {
            var key = RandomNumberGenerator.GetBytes(16); // 16 bytes instead of 32
            Assert.Throws<ArgumentException>(() => new AesEncryptionService(key));
        }

        [Fact]
        public void Decrypt_TooShortCiphertext_Throws()
        {
            var service = CreateService();
            var tooShort = Convert.ToBase64String(new byte[10]); // < nonce(12) + tag(16)

            Assert.Throws<ArgumentException>(() => service.Decrypt(tooShort));
        }

        [Fact]
        public void EncryptBytes_Then_DecryptBytes_ReturnsOriginal()
        {
            var service = CreateService();
            var original = RandomNumberGenerator.GetBytes(256); // binary content, e.g. an attachment

            var cipher = service.EncryptBytes(original);
            var result = service.DecryptBytes(cipher);

            Assert.Equal(original, result);
        }
    }
}
