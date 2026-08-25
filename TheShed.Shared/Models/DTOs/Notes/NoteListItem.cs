namespace TheShed.Shared.Models.DTOs.Notes
{
    /// <summary>Metadata of a secure note for listings. Title is AES-256-GCM ciphertext (vault
    /// key, Sprint 26) — the caller decrypts it client-side right after fetching, to display and
    /// to sort locally (the server can no longer sort on ciphertext). Carries no content; the
    /// content ciphertext is fetched one note at a time via GET /api/notes/{id}.</summary>
    public class NoteListItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsFavorite { get; set; }
    }
}
