namespace TheShed.Shared.Models.DTOs.Notes
{
    /// <summary>Detail of a single secure note. Title and Content are both AES-256-GCM ciphertext
    /// blobs as stored (vault key) — the server cannot decrypt either of them (Sprint 26); the
    /// caller unwraps each client-side. Returned only when requesting an individual note
    /// (GET /api/notes/{id}), never in listings.</summary>
    public class NoteResponse
    {
        public int Id { get; set; }
        public int VaultId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsFavorite { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
