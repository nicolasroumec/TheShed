using System.ComponentModel.DataAnnotations;

namespace TheShed.Shared.Models.DTOs.Entries
{
    /// <summary>Data to edit an existing entry. Does not include VaultId: an entry
    /// does not change vault when edited. The password replaces the previous one.</summary>
    public class EntryUpdateRequest
    {
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
