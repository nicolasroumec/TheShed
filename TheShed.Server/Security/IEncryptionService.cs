namespace TheShed.Server.Security
{
    /// <summary>
    /// Cifra y descifra los valores sensibles de las entradas (p. ej.
    /// <c>PasswordEntry.PasswordEncrypted</c>) con AES-256-GCM.
    /// El texto cifrado resultante incluye el nonce y el tag de autenticación
    /// embebidos, por lo que no hace falta almacenarlos por separado.
    /// </summary>
    public interface IEncryptionService
    {
        /// <summary>
        /// Cifra texto plano. Devuelve <c>base64(nonce || ciphertext || tag)</c>.
        /// </summary>
        string Encrypt(string plaintext);

        /// <summary>
        /// Descifra un valor producido por <see cref="Encrypt"/>.
        /// </summary>
        /// <exception cref="System.Security.Cryptography.AuthenticationTagMismatchException">
        /// Si el dato fue manipulado o la clave no coincide.
        /// </exception>
        string Decrypt(string ciphertext);
    }
}
