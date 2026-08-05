namespace TheShed.Shared.Models.DTOs.Attachments
{
    /// <summary>Metadata for an entry attachment. Content is fetched separately via download.</summary>
    public class AttachmentResponse
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
