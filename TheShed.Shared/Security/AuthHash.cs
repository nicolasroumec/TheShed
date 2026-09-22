using System.Security.Cryptography;

namespace TheShed.Shared.Security
{
    /// <summary>
    /// What the client sends as "password" on register/login instead of the master password
    /// (N1 in FEATURES-ROADMAP.md). The server Argon2-hashes this value like any password, but
    /// can't go back from it to the stretched master key — HMAC is one-way — so knowing what
    /// arrives on the wire is no longer enough to decrypt the account's private key.
    /// </summary>
    public static class AuthHash
    {
        public static string Compute(byte[] stretchedMasterKey) =>
            Convert.ToBase64String(HMACSHA256.HashData(stretchedMasterKey, "theshed-auth-hash"u8));
    }
}
