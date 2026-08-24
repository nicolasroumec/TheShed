using TheShed.Shared.Models.DTOs.Tags;

namespace TheShed.Shared.Models.DTOs.Entries
{
    /// <summary>Lightweight entry shape for listings. Carries only metadata and
    /// never the password; the password ciphertext (decrypted client-side) is
    /// revealed one at a time via GET /api/entries/{id}.</summary>
    public class EntryListItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Url { get; set; }
        public bool IsFavorite { get; set; }
        public List<TagResponse> Tags { get; set; } = [];

        /// <summary>When the current password became active: the most recent
        /// EntryHistory snapshot's CreatedAt, or the entry's own CreatedAt if
        /// the password has never changed.</summary>
        public DateTime PasswordChangedAt { get; set; }
    }
}
