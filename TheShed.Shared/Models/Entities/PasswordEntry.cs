using TheShed.Shared.Models.Base;

namespace TheShed.Shared.Models.Entities
{
    public class PasswordEntry : AuditableEntity
    {
        public int Id { get; set; }
        public int VaultId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PasswordEncrypted { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? Notes { get; set; }
        public bool IsFavorite { get; set; } = false;

        public Vault Vault { get; set; } = null!;
        public ICollection<PasswordEntryTag> Tags { get; set; } = [];
        public ICollection<EntryHistory> History { get; set; } = [];
        public ICollection<Attachment> Attachments { get; set; } = [];
    }
}
