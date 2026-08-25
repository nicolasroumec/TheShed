using System.ComponentModel.DataAnnotations;

namespace TheShed.Shared.Models.DTOs.Notes
{
    /// <summary>Data to edit an existing secure note. Does not include VaultId: a note
    /// does not change vault when edited. Title and Content are ciphertext (see
    /// NoteCreateRequest) and each replaces the previous blob.</summary>
    public class NoteUpdateRequest
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public bool IsFavorite { get; set; }
    }
}
