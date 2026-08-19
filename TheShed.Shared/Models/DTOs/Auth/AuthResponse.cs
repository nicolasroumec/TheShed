namespace TheShed.Shared.Models.DTOs.Auth
{
    public class AuthResponse
    {
        public DateTime ExpiresAt { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Null for users who registered before Sprint 25 and haven't been migrated yet.
        public string? KeySalt { get; set; }
    }
}
