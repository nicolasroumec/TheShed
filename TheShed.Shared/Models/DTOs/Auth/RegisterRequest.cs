using System.ComponentModel.DataAnnotations;

namespace TheShed.Shared.Models.DTOs.Auth
{
    public class RegisterRequest
    {
        [Required, MinLength(3), MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        // Argon2 cost grows with the input, so an unbounded password is a cheap way to burn
        // server CPU. 128 chars is far above any real master password.
        [Required, MinLength(8), MaxLength(128)]
        public string Password { get; set; } = string.Empty;

        // Generated client-side (IKeyDerivationService.GenerateSalt, base64) and attached by
        // AuthService before the request goes out — never typed by the user.
        [Required]
        public string KeySalt { get; set; } = string.Empty;
    }
}
