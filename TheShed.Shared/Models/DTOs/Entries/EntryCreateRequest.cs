using System.ComponentModel.DataAnnotations;

namespace TheShed.Shared.Models.DTOs.Entries
{
    /// <summary>Data to create an entry. The password travels in plaintext and the
    /// server encrypts it before persisting.</summary>
    public class EntryCreateRequest
    {
        [Required]
        public int VaultId { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [MaxLength(2048)]
        public string? Url { get; set; }

        public string? Notes { get; set; }

        public bool IsFavorite { get; set; }
    }
}
