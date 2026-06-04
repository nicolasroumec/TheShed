namespace TheShed.Server.Security
{
    /// <summary>
    /// Abstrae el hashing de la contraseña maestra del usuario.
    /// El hash resultante incluye el salt y los parámetros embebidos.
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>Genera el hash de una contraseña en texto plano.</summary>
        string Hash(string password);

        /// <summary>Verifica que una contraseña en texto plano coincida con un hash existente.</summary>
        bool Verify(string password, string hash);
    }
}
