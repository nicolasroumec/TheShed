namespace TheShed.Shared.Models.DTOs.Entries
{
    /// <summary>Lightweight entry shape for listings. Carries only metadata and
    /// never the password; the plaintext password is revealed one at a time via
    /// GET /api/entries/{id}.</summary>
    public class EntryListItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Url { get; set; }
        public bool IsFavorite { get; set; }
    }
}
