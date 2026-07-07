using TheShed.Shared.Models.DTOs.Tags;

namespace TheShed.Shared.Models.DTOs.Entries
{
    /// <summary>Detail of a single entry. Includes the decrypted password; returned
    /// only when requesting an individual entry (GET /api/entries/{id}),
    /// never in listings.</summary>
    public class EntryResponse
    {
        public int Id { get; set; }
        public int VaultId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? Notes { get; set; }
        public bool IsFavorite { get; set; }
        public List<TagResponse> Tags { get; set; } = [];
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
