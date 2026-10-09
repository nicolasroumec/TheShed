using Isopoh.Cryptography.Argon2;

namespace TheShed.Server.Security
{
    /// <summary>
    /// <see cref="IPasswordHasher"/> implementation using Argon2.
    /// The resulting hash follows the PHC format (salt and parameters included),
    /// so the salt does not need to be stored separately.
    /// </summary>
    public class Argon2PasswordHasher : IPasswordHasher
    {
        public string Hash(string password)
        {
            ArgumentException.ThrowIfNullOrEmpty(password);
            return Argon2.Hash(password);
        }

        public bool Verify(string password, string hash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
            {
                return false;
            }

            return Argon2.Verify(hash, password);
        }
    }
}
