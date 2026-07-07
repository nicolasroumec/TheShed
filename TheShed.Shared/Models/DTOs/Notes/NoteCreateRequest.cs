using System.ComponentModel.DataAnnotations;

namespace TheShed.Shared.Models.DTOs.Notes
{
    /// <summary>Data to create a secure note. The content travels in plaintext and the
    /// server encrypts it before persisting.</summary>
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
