using System.ComponentModel.DataAnnotations;

namespace TheShed.Shared.Models.DTOs.Entries
{
    /// <summary>Data to create an entry. Name, Username, Password and Url are all AES-256-GCM
    /// ciphertext (vault key), encrypted client-side before this request goes out — the server
    /// only ever stores and returns each blob as-is (Sprint 26). No MaxLength on them: a
    /// ciphertext blob runs longer than the plaintext it holds (nonce + tag + base64 overhead),
    /// so a cap sized for the old plaintext fields no longer means anything.</summary>
    public class EntryCreateRequest
    {
        [Required]
        public int VaultId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public string? Url { get; set; }

        public string? Notes { get; set; }

        public bool IsFavorite { get; set; }
    }
}
