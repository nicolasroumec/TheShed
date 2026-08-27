namespace TheShed.Shared.Models.DTOs.Users
{
    /// <summary>A user's RSA public key, looked up by email to wrap a vault key for them
    /// when sharing a vault (Sprint 27).</summary>
    public class UserPublicKeyResponse
    {
        public int UserId { get; set; }
        public string PublicKey { get; set; } = string.Empty;
    }
}
