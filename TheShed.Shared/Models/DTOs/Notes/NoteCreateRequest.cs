using System.ComponentModel.DataAnnotations;

namespace TheShed.Shared.Models.DTOs.Notes
{
    /// <summary>Data to create a secure note. Content is AES-256-GCM ciphertext (vault key),
    /// encrypted client-side before this request goes out — the server only ever stores and
    /// returns the blob as-is (Sprint 26).</summary>
    public class NoteCreateRequest
    {
        [Required]
        public int VaultId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public bool IsFavorite { get; set; }
    }
}
