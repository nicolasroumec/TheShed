using System.ComponentModel.DataAnnotations;

namespace TheShed.Shared.Models.DTOs.Notes
{
    /// <summary>Data to edit an existing secure note. Does not include VaultId: a note
    /// does not change vault when edited. The content replaces the previous one.</summary>
    public class NoteUpdateRequest
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public bool IsFavorite { get; set; }
    }
}
