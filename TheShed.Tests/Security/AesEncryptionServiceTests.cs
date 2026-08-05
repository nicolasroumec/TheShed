using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using TheShed.Server.Security;
using Xunit;

namespace TheShed.Tests.Security
{
    public class AesEncryptionServiceTests
    {
        private static AesEncryptionService CreateService()
        {
            var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var settings = Options.Create(new EncryptionSettings { Key = key });
            return new AesEncryptionService(settings);
        }

        [Theory]
        [InlineData("contraseña-super-secreta")]
        [InlineData("")]
        [InlineData("áéíóú 🔐 con símbolos & espacios")]
        public void Encrypt_Then_Decrypt_DevuelveOriginal(string plaintext)
        {
            var service = CreateService();

            var cipher = service.Encrypt(plaintext);
            var result = service.Decrypt(cipher);

            Assert.Equal(plaintext, result);
        }

        [Fact]
        public void Encrypt_MismoTexto_ProduceCifradosDistintos()
        {
            var service = CreateService();

            var a = service.Encrypt("misma-clave");
            var b = service.Encrypt("misma-clave");

            // Nonce aleatorio por operación → los cifrados no deben coincidir.
            Assert.NotEqual(a, b);
        }

        [Fact]
        public void Decrypt_DatoManipulado_Lanza()
        {
            var service = CreateService();
            var cipher = service.Encrypt("dato-integro");

            // Alterar un byte del payload (flip del último byte del tag).
            var bytes = Convert.FromBase64String(cipher);
            bytes[^1] ^= 0xFF;
            var tampered = Convert.ToBase64String(bytes);

            Assert.Throws<AuthenticationTagMismatchException>(() => service.Decrypt(tampered));
        }

        [Fact]
        public void Decrypt_ConOtraClave_Lanza()
        {
            var cipher = CreateService().Encrypt("secreto");
            var otroServicio = CreateService(); // clave distinta

            Assert.Throws<AuthenticationTagMismatchException>(() => otroServicio.Decrypt(cipher));
        }

        [Fact]
        public void Constructor_ClaveVacia_Lanza()
        {
            var settings = Options.Create(new EncryptionSettings { Key = "" });
            Assert.Throws<InvalidOperationException>(() => new AesEncryptionService(settings));
        }

        [Fact]
        public void Constructor_ClaveLongitudIncorrecta_Lanza()
        {
            // 16 bytes en vez de 32.
            var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
            var settings = Options.Create(new EncryptionSettings { Key = key });

            Assert.Throws<InvalidOperationException>(() => new AesEncryptionService(settings));
        }

        [Fact]
        public void Decrypt_TextoDemasiadoCorto_Lanza()
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
