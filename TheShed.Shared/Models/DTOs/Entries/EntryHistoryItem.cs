namespace TheShed.Shared.Models.DTOs.Entries
{
    /// <summary>Metadata for one past password of an entry. The decrypted password
    /// is revealed one at a time via GET /api/entries/{id}/history/{historyId}.</summary>
    public class EntryHistoryItem
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ChangedByUsername { get; set; } = string.Empty;
    }
}
