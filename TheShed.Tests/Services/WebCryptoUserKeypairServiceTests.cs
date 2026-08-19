using System.Security.Cryptography;
using TheShed.Client.Services;
using Xunit;

namespace TheShed.Tests.Services
{
    public class WebCryptoUserKeypairServiceTests
    {
        [Fact]
        public void FormatPublicKeyPem_ProducesPemImportableAsTheOriginalKey()
        {
            using var rsa = RSA.Create(3072);
            var spkiBase64 = Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());

            var pem = WebCryptoUserKeypairService.FormatPublicKeyPem(spkiBase64);

            using var imported = RSA.Create();
            imported.ImportFromPem(pem);
            Assert.Equal(rsa.ExportSubjectPublicKeyInfo(), imported.ExportSubjectPublicKeyInfo());
        }
    }
}
