namespace TheShed.Shared.Models.DTOs.Entries
{
    /// <summary>A single past password, as its AES-256-GCM ciphertext blob (vault key) — the
    /// server cannot decrypt it (Sprint 26); the caller unwraps it client-side. Returned only
    /// when requesting one history entry (GET /api/entries/{id}/history/{historyId}).</summary>
    public class EntryHistoryDetail
    {
        public int Id { get; set; }
        public string Password { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string ChangedByUsername { get; set; } = string.Empty;
    }
}
