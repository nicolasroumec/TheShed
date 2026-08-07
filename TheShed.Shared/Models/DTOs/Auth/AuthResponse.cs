namespace TheShed.Shared.Models.DTOs.Auth
{
    public class AuthResponse
    {
        public DateTime ExpiresAt { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
