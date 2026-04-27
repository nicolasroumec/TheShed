using TheShed.Shared.Models.Base;

namespace TheShed.Shared.Models.Entities
{
    public class EntryHistory : AuditableEntity
    {
        public int Id { get; set; } 
        public int PasswordEntryId { get; set; }
        public string PasswordEncrypted { get; set; } = string.Empty;
    }
}
