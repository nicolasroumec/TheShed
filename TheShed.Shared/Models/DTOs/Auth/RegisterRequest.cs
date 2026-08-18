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

        // Also generated client-side (IUserKeypairService.Generate) and attached by AuthService.
        // EncryptedPrivateKey is wrapped with the stretched master key before it ever leaves
        // the browser — the server stores it as an opaque blob.
        [Required]
        public string PublicKey { get; set; } = string.Empty;

        [Required]
        public string EncryptedPrivateKey { get; set; } = string.Empty;
    }
}
