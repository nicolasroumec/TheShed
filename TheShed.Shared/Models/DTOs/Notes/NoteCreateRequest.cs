using System.ComponentModel.DataAnnotations;

namespace TheShed.Shared.Models.DTOs.Notes
{
    /// <summary>Data to create a secure note. Title and Content are both AES-256-GCM ciphertext
    /// (vault key), encrypted client-side before this request goes out — the server only ever
    /// stores and returns each blob as-is (Sprint 26). No MaxLength on Title: a ciphertext blob
    /// runs longer than the plaintext it holds (nonce + tag + base64 overhead), so a cap sized
    /// for the old plaintext field no longer means anything.</summary>
    public class NoteCreateRequest
    {
        [Required]
        public int VaultId { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public bool IsFavorite { get; set; }
    }
}
