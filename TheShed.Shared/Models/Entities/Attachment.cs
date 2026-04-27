using TheShed.Shared.Models.Base;

namespace TheShed.Shared.Models.Entities
{
    public class Attachment : AuditableEntity
    {
        public int Id { get; set; }
        public int PasswordEntryId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public int FileSizeBytes { get; set; }
    }
}