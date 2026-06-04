using Isopoh.Cryptography.Argon2;

namespace TheShed.Server.Security
{
    /// <summary>
    /// Implementación de <see cref="IPasswordHasher"/> usando Argon2.
    /// El hash producido sigue el formato PHC (incluye salt y parámetros),
    /// por lo que no es necesario almacenar el salt por separado.
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
