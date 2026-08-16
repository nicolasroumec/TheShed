using System.ComponentModel.DataAnnotations;

namespace TheShed.Shared.Models.DTOs.Auth
{
    public class LoginRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        // Same cap as RegisterRequest: login also feeds Argon2, and it is the endpoint an
        // unauthenticated caller can hit freely.
        [Required, MaxLength(128)]
        public string Password { get; set; } = string.Empty;
    }
}
