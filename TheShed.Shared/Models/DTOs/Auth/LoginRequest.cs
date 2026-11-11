using System.ComponentModel.DataAnnotations;

namespace TheShed.Shared.Models.DTOs.Auth
{
    public class LoginRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        // The login form binds the master password here, but what goes on the wire is
        // AuthHash.Compute(stretched key) — client AuthService sends a copy. Same cap as
        // RegisterRequest: login also feeds Argon2, and anyone can hit it unauthenticated.
        [Required, MaxLength(128)]
        public string Password { get; set; } = string.Empty;

        // Second login step (Sprint 18): empty on the first attempt. If the account has 2FA,
        // the server answers 401 with twoFactorRequired and the client resends with the code.
        [MaxLength(6)]
        public string? TotpCode { get; set; }
    }
}
