namespace TheShed.Shared.Models.DTOs.Notes
{
    /// <summary>Metadata of a secure note for listings. Carries no content; the content
    /// ciphertext (decrypted client-side) is fetched one note at a time via GET /api/notes/{id}.</summary>
    public class NoteListItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsFavorite { get; set; }
    }
}
