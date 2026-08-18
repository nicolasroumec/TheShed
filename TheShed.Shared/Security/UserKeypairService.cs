using System.Security.Cryptography;

namespace TheShed.Shared.Security
{
    /// <inheritdoc cref="IUserKeypairService"/>
    public class UserKeypairService : IUserKeypairService
    {
        private const int KeySizeBits = 3072;

        public UserKeypair Generate(byte[] stretchedMasterKey)
        {
            ArgumentNullException.ThrowIfNull(stretchedMasterKey);

            using var rsa = RSA.Create(KeySizeBits);
            var publicKeyPem = rsa.ExportSubjectPublicKeyInfoPem();
            var privateKeyBytes = rsa.ExportPkcs8PrivateKey();

            var wrapper = new AesEncryptionService(stretchedMasterKey);
            var encryptedPrivateKey = Convert.ToBase64String(wrapper.EncryptBytes(privateKeyBytes));

            return new UserKeypair(publicKeyPem, encryptedPrivateKey);
        }
    }
}
