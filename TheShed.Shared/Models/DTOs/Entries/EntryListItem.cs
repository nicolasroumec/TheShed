using TheShed.Shared.Models.DTOs.Tags;

namespace TheShed.Shared.Models.DTOs.Entries
{
    /// <summary>Lightweight entry shape for listings. Name, Username and Url are AES-256-GCM
    /// ciphertext (vault key, Sprint 26) — the caller decrypts them client-side right after
    /// fetching, to display and to search/sort locally (the server can no longer do either on
    /// ciphertext). Never carries the password; that ciphertext is fetched and decrypted one
    /// entry at a time via GET /api/entries/{id}.</summary>
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
