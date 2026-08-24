using System.ComponentModel.DataAnnotations;

namespace TheShed.Shared.Models.DTOs.Entries
{
    /// <summary>Data to create an entry. Password is AES-256-GCM ciphertext (vault key),
    /// encrypted client-side before this request goes out — the server only ever stores and
    /// returns the blob as-is (Sprint 26).</summary>
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
