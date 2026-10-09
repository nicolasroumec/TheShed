namespace TheShed.Server.Security
{
    /// <summary>
    /// Abstracts hashing of the user's master password.
    /// The resulting hash embeds the salt and parameters.
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>Hashes a plaintext password.</summary>
        string Hash(string password);

        /// <summary>Checks whether a plaintext password matches an existing hash.</summary>
        bool Verify(string password, string hash);
    }
}
