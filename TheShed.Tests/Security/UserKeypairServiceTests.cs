using System.Security.Cryptography;
using TheShed.Shared.Security;
using Xunit;

namespace TheShed.Tests.Security
{
    public class UserKeypairServiceTests
    {
        private readonly UserKeypairService _sut = new();

        [Fact]
        public void Generate_PublicKey_RoundtripsWithDecryptedPrivateKey()
        {
            var stretchedMasterKey = RandomNumberGenerator.GetBytes(32);
            var keypair = _sut.Generate(stretchedMasterKey);

            var privateKeyBytes = new AesEncryptionService(stretchedMasterKey)
                .DecryptBytes(Convert.FromBase64String(keypair.EncryptedPrivateKey));

            using var publicRsa = RSA.Create();
            publicRsa.ImportFromPem(keypair.PublicKeyPem);
            using var privateRsa = RSA.Create();
            privateRsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);

            var plaintext = RandomNumberGenerator.GetBytes(32); // e.g. a vault key
            var sealedBlob = publicRsa.Encrypt(plaintext, RSAEncryptionPadding.OaepSHA256);
            var opened = privateRsa.Decrypt(sealedBlob, RSAEncryptionPadding.OaepSHA256);

            Assert.Equal(plaintext, opened);
        }

        [Fact]
        public void Generate_EncryptedPrivateKey_WrongMasterKey_FailsToDecrypt()
        {
            var keypair = _sut.Generate(RandomNumberGenerator.GetBytes(32));
            var wrongKey = new AesEncryptionService(RandomNumberGenerator.GetBytes(32));

            Assert.Throws<AuthenticationTagMismatchException>(
                () => wrongKey.DecryptBytes(Convert.FromBase64String(keypair.EncryptedPrivateKey)));
        }
    }
}
